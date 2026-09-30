using System.Data;
using AutoMapper;
using ClosedXML.Excel;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using ManejoPresupuesto.Hubs;

namespace ManejoPresupuesto.Controllers
{

    // Esta clase mantendrá en memoria quién está usando la web en este momento
    public static class MemoriaAsistente
    {
        public static int UsuarioActivoId { get; set; } = 0;
    }
     
    public class TransaccionesController: Controller
    {
         private readonly IServiciosUsuarios serviciosUsuarios;
         private readonly IRepositorioCuenta repositorioCuenta;
         
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IMapper mapper;
        private readonly IServiciosReportes serviciosReportes;

        public TransaccionesController(IServiciosUsuarios serviciosUsuarios, 
        IRepositorioCuenta repositorioCuenta, IRepositorioCategorias repositorioCategorias,
        IRepositorioTransacciones repositorioTransacciones, IMapper mapper, IServiciosReportes serviciosReportes)
        {
            this.serviciosUsuarios = serviciosUsuarios;
            this.repositorioCuenta = repositorioCuenta;
            this.repositorioCategorias = repositorioCategorias;
            this.repositorioTransacciones = repositorioTransacciones;
            this.mapper = mapper;
            this.serviciosReportes = serviciosReportes;
        }
       
         public async Task<IActionResult> Index(int mes, int año)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();

            MemoriaAsistente.UsuarioActivoId = usuarioId;

            var modelo = await serviciosReportes.ObtenerReporteTransaccionesDetalladas(usuarioId, mes, año, ViewBag);

            return View(modelo);
        }
  
        public async Task<IActionResult> Semanal(int mes, int año)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana = await serviciosReportes.ObtenerReporteSemanal(usuarioId, 
            mes, año, ViewBag);
            var agrupado = transaccionesPorSemana.GroupBy(x => x.Semana).Select(x =>new ResultadoObtenerPorSemana()
            {
                Semana=x.Key,
                ingreso = x.Where(x => x.tipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                gastos = x.Where(x => x.tipoOperacionId == TipoOperacion.Gastos).Select(x => x.Monto).FirstOrDefault()
            } ).ToList();

            if(año == 0 || mes == 0)
            {
                var hoy = DateTime.Today;
                año = hoy.Year;
                mes = hoy.Month;
            }

            var fechaReferencia = new DateTime(año, mes, 1);
            var diasDelMes = Enumerable.Range(1, fechaReferencia.AddMonths(1).AddDays(-1).Day);

            var diasSegmentados = diasDelMes.Chunk(7).ToList();
            var modelo = new ReporteSemanalViewModel();

            for(int i = 0; i< diasSegmentados.Count(); i++)
            {
                var semana = i + 1;
                var fechaInicio = new DateTime(año, mes, diasSegmentados[i].First());
                var fechaFin = new DateTime(año, mes, diasSegmentados[i].Last());
                var grupoSemana = agrupado.FirstOrDefault(x=> x.Semana == semana);

                if(grupoSemana is null)
                {
                    agrupado.Add( new ResultadoObtenerPorSemana()
                    {
                        Semana = semana,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    }); 
                } else
                {
                    grupoSemana.FechaInicio = fechaInicio;
                    grupoSemana.FechaFin = fechaFin;
                }

                agrupado = agrupado.OrderByDescending(x => x.Semana).ToList();

                
                modelo.TransaccionesPorSemana = agrupado;
                modelo.FechaReferencia = fechaReferencia;
            }
             return View(modelo);   
        }
        
         public async Task<IActionResult> Mensual(int año)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            if(año == 0)
            {
                año= DateTime.Today.Year;
            }

            var transaccionesPorMes = await repositorioTransacciones.ObtenerPorMes(usuarioId, año);
            var transaccionesAgrupadas =transaccionesPorMes.GroupBy(x=> x.Mes).Select(x=> new ResultadoObtenerPorMes()
            {
                Mes = x.Key,
                Ingreso = x.Where(x => x.TipoOperacionId== TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gasto = x.Where(x => x.TipoOperacionId== TipoOperacion.Gastos).Select(x => x.Monto).FirstOrDefault()
            }).ToList();

            for (int mes=1; mes<=12; mes++)
            {
                var transaccion = transaccionesAgrupadas.FirstOrDefault(x=> x.Mes == mes);
                var fechaReferencia = new DateTime(año, mes, 1);

                if(transaccion is null)
                {
                    transaccionesAgrupadas.Add(new ResultadoObtenerPorMes()
                    {
                        Mes = mes,
                        FechaReferencia = fechaReferencia
                    });
                }
                else
                {
                    transaccion.FechaReferencia= fechaReferencia;
                }
            }
            transaccionesAgrupadas = transaccionesAgrupadas.OrderByDescending(x=> x.Mes).ToList();

            var modelo = new ReporteMensualViewModel();
            modelo.Año = año;
            modelo.TransaccionesPorMes = transaccionesAgrupadas;


            return View(modelo);
        }
        
         public IActionResult ExcelReporte()
        {
            return View();
        }
        [HttpGet]
        public async Task<FileResult> ExportarExcelPorMes(int mes, int año)
        {
            var fechaInicio = new DateTime(año, mes, 1);
            var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId( new ParametroObtenerTransaccionesPorUsuario
            {
                usuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });
            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("MMM yyyy")}.xlsx";

            return GenerarExcel(nombreArchivo, transacciones);
        }

        [HttpGet]

        public async Task<FileResult> ExportarExcelPorAño(int año)
        {
            var fechaInicio = new DateTime(año, 1, 1);
            var fechaFin = fechaInicio.AddYears(1).AddDays(-1);
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

             var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId( new ParametroObtenerTransaccionesPorUsuario
            {
                usuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("yyyy")}.xlsx";

            return GenerarExcel(nombreArchivo, transacciones);
        }

        public async Task<FileResult> ExportarExcelTodo()
        {
            var fechaInicio = DateTime.Today.AddYears(-100);
            var fechaFin = DateTime.Today.AddYears(1000);
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId( new ParametroObtenerTransaccionesPorUsuario
            {
                usuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });
            var nombreArchivo = $"Manejo Presupuesto- {DateTime.Today.ToString("dd-MM-yyyy")}.xlsx";

            return GenerarExcel(nombreArchivo, transacciones);
        }

        private FileResult GenerarExcel(string nombreArchivo, IEnumerable<Transaccion> transacciones)
        {
            DataTable dataTable = new DataTable("Transacciones");
            dataTable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Fecha"),
                new DataColumn("Cuenta"),
                new DataColumn("Categoria"),
                new DataColumn("Nota"),
                new DataColumn("Monto"),
                new DataColumn("Ingreso/Gasto"),
                
            });
            foreach (var transaccion in transacciones)
            {
                dataTable.Rows.Add(transaccion.FechaTransaccion, 
                                    transaccion.Cuenta, 
                                    transaccion.Categoria, 
                                    transaccion.Nota,
                                    transaccion.Monto,
                                    transaccion.TipoOperacionId);
            }
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dataTable);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
                }
            } 
        }
    

        public IActionResult Calendario()
        {
            return View();
        }

        public async Task<JsonResult> ObtenerTransaccionesCalendario(DateTime start, DateTime end)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
             var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId( new ParametroObtenerTransaccionesPorUsuario
            {
                usuarioId = usuarioId,
                FechaInicio = start,
                FechaFin = end
            });

            var eventoCalendario = transacciones.Select(transaccion => new EventoCalendario()
            {
                Title = transaccion.Monto.ToString("N"),
                Start = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                End = transaccion.FechaTransaccion.ToString("yyyy-MM-dd"),
                Color = (transaccion.TipoOperacionId == TipoOperacion.Gastos) ? "Red": null
            });

            return Json(eventoCalendario);
        }

        public async Task<IActionResult> ObtenerTransaccionesPorFecha(DateTime fecha)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId( new ParametroObtenerTransaccionesPorUsuario
            {
                usuarioId = usuarioId,
                FechaInicio = fecha,
                FechaFin = fecha
            });

            return Json(transacciones);
        }        
        
        public async Task<IActionResult> Crear()
        {
             var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
             var modelo = new TransaccionCreacionViewModel();
             modelo.Cuentas = await ObtenerCuentas(usuarioId);
             modelo.Categoria = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
             return View(modelo);
        }
        [HttpPost]
        public async Task<IActionResult> Crear(TransaccionCreacionViewModel modelo)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();

            if (!ModelState.IsValid)
            {
               modelo.Cuentas = await ObtenerCuentas(usuarioId);
               modelo.Categoria = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
               return View(modelo);
            }

            var cuenta = await repositorioCuenta.obtenerPorId(modelo.CuentaId, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            modelo.UsuarioId = usuarioId;

            if(modelo.TipoOperacionId == TipoOperacion.Gastos)
            {
                modelo.Monto *= -1;
            }
            await repositorioTransacciones.Crear(modelo);
            return RedirectToAction("index");

        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, string urlRetorno = null)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = mapper.Map<TransaccionActualizacionViewModel>(transaccion);
           
            if (modelo.TipoOperacionId == TipoOperacion.Gastos)
            {
                modelo.montoAnterior = modelo.Monto * -1;
            }

            modelo.cuentaAnteriorId = transaccion.CuentaId;
            modelo.Categoria = await ObtenerCategorias(usuarioId, transaccion.TipoOperacionId);
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.urlRetorno = urlRetorno;
            return View(modelo);
        }
        [HttpPost]
        public async Task<IActionResult> Editar(TransaccionActualizacionViewModel modelo)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();

            if(!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categoria = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuenta.obtenerPorId(modelo.CuentaId, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            
            if(categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            var transaccion = mapper.Map<Transaccion>(modelo);

            modelo.montoAnterior = modelo.Monto;

            if(modelo.TipoOperacionId == TipoOperacion.Gastos)
            {
                transaccion.Monto *= -1;
            }
            await repositorioTransacciones.Actualizar(transaccion, modelo.montoAnterior, modelo.cuentaAnteriorId);

            if (string.IsNullOrEmpty(modelo.urlRetorno))
            {
                  return RedirectToAction("Index");
            } else
            {
                return LocalRedirect(modelo.urlRetorno);
            }
          
        }

        [HttpPost] 
        public async Task<IActionResult> Borrar(int id, string urlRetorno = null)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if(transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTransacciones.Borrar(id);

            if (string.IsNullOrEmpty(urlRetorno))
            {
                  return RedirectToAction("Index");
            } else
            {
                return LocalRedirect(urlRetorno);
            }

        } 
        [HttpGet]
        public async Task<IActionResult> ObtenerDetalles(int id)
        {
            // Va a buscar los productos a la base de datos
            var detalles = await repositorioTransacciones.ObtenerDetallesPorTransaccionId(id);
            
            // Los devuelve en formato JSON para que JavaScript los pueda dibujar en pantalla
            return Json(detalles);
        }
        private async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
        {
             var cuentas = await repositorioCuenta.Buscar(usuarioId);
             return cuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }
        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, TipoOperacion tipoOperacion)
        {
            var categorias = await repositorioCategorias.Obtener(usuarioId, tipoOperacion);
            var resultado = categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString())).ToList();

            var opcionPorDefecto = new SelectListItem("-- Seleccione Una Categoria --", "0", true);

            resultado.Insert(0, opcionPorDefecto);
            return resultado;
        }
        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var categorias = await ObtenerCategorias(usuarioId, tipoOperacion);
            return Ok(categorias);
        }

       // Asegúrate de agregar este using arriba del todo en el archivo:
        // using Microsoft.AspNetCore.SignalR;
        // using ManejoPresupuesto.Hubs;

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarDesdeVoz(
            [FromBody] Transaccion transaccion, 
            [FromServices] IHubContext<TransaccionesHub> hubContext) 
        {
            int usuarioIdActual = MemoriaAsistente.UsuarioActivoId;
            if (usuarioIdActual == 0) return BadRequest("Por favor, entra a la página web primero.");

            var cuentas = await repositorioCuenta.Buscar(usuarioIdActual);
            var cuentaDefault = cuentas.FirstOrDefault();
            if (cuentaDefault == null) return BadRequest("El usuario no tiene cuentas.");
            
            // 1. Ya no forzamos Gastos, usamos el Tipo que mandó Python
            var tipoOperacion = transaccion.TipoOperacionId == 0 ? TipoOperacion.Gastos : transaccion.TipoOperacionId;

            // 2. Buscamos una categoría que coincida con ese tipo (Ingreso o Gasto)
            var categorias = await repositorioCategorias.Obtener(usuarioIdActual, tipoOperacion);
            var categoriaDefault = categorias.FirstOrDefault();
            if (categoriaDefault == null) return BadRequest($"El usuario no tiene categorías para {tipoOperacion}.");

            transaccion.UsuarioId = usuarioIdActual; 
            transaccion.CuentaId = cuentaDefault.Id;
            transaccion.CategoriaId = categoriaDefault.Id;
            transaccion.FechaTransaccion = DateTime.Now;
            
            // 3. Lógica matemática: Positivo para ingresos, Negativo para gastos
            if (tipoOperacion == TipoOperacion.Gastos)
            {
                transaccion.Monto = Math.Abs(transaccion.Monto) * -1; 
            }
            else
            {
                transaccion.Monto = Math.Abs(transaccion.Monto); // Ingresos quedan positivos
            }

            await repositorioTransacciones.Crear(transaccion);
            await hubContext.Clients.All.SendAsync("NuevaTransaccion");

            return Ok(new { mensaje = "Movimiento registrado exitosamente." });
        }
       
        public async Task<IActionResult> CartolaMensual()
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId(); // Ajusta esto según tu método para obtener el ID
            var fechaActual = DateTime.Today;
            
            var cartola = await repositorioTransacciones.ObtenerCartolaMensual(usuarioId, fechaActual.Month, fechaActual.Year);
            
            ViewBag.Mes = fechaActual.ToString("MMMM yyyy").ToUpper();
            return View(cartola);
        }

            [HttpPost]
            [AllowAnonymous]
            public async Task<IActionResult> RegistrarBoletaWhatsApp([FromBody] BoletaWhatsAppRequest request)
            {
                var usuarioId = 11; // Mantenemos tu usuario fijo
                
                // 1. ASIGNACIÓN DINÁMICA DE LA CUENTA
                var cuentas = await repositorioCuenta.Buscar(usuarioId);
                var cuentaDefault = cuentas.FirstOrDefault();
                
                if (cuentaDefault == null) 
                {
                    return BadRequest("El usuario no tiene cuentas creadas.");
                }

                // 2. OBTENER CATEGORÍAS ACTUALES
                var categorias = await repositorioCategorias.Obtener(usuarioId, TipoOperacion.Gastos);
                
                // 3. BUSCAR COINCIDENCIA CON LA IA
                // Comparamos lo que dedujo Gemini con tus categorías reales
                var categoriaMatch = categorias.FirstOrDefault(c => 
                    !string.IsNullOrEmpty(request.CategoriaSugerida) && 
                    c.Nombre.ToLower().Contains(request.CategoriaSugerida.ToLower())
                );

                int categoriaIdFinal;

                // 4. LÓGICA DE CREACIÓN DINÁMICA DE CATEGORÍA
                if (categoriaMatch != null)
                {
                    // Si ya existe, usamos su ID
                    categoriaIdFinal = categoriaMatch.Id;
                }
                else
                {
                    // SI NO EXISTE: ¡La creamos al vuelo!
                    
                    // Si la IA no mandó categoría o la mandó vacía, le ponemos un nombre por defecto
                    string nombreNuevaCategoria = string.IsNullOrEmpty(request.CategoriaSugerida) 
                                                ? "Otros (IA)" 
                                                : request.CategoriaSugerida;

                    var nuevaCategoria = new Categoria()
                    {
                        Nombre = nombreNuevaCategoria,
                        TipoOperacionId = TipoOperacion.Gastos,
                        UsuarioId = usuarioId
                        // Si tu modelo de Categoria requiere otros campos (como un ícono), agrégalos aquí.
                    };

                    // Guardamos la nueva categoría en la base de datos
                    await repositorioCategorias.Crear(nuevaCategoria);

                    // Como Dapper (en tu método Crear) seguramente ya actualiza el 'Id' del objeto 'nuevaCategoria',
                    // tomamos ese nuevo ID generado.
                    categoriaIdFinal = nuevaCategoria.Id;
                }

                // 5. CONSTRUCCIÓN DE LA TRANSACCIÓN
                var transaccion = new Transaccion()
                {
                    UsuarioId = usuarioId,
                    Monto = Math.Abs(request.MontoTotal) * -1, 
                    Nota = request.Descripcion,
                    CuentaId = cuentaDefault.Id,       
                    CategoriaId = categoriaIdFinal,  // <-- Asignado (ya sea el existente o el recién creado)
                    FechaTransaccion = DateTime.Now
                };

                // 6. GUARDAR LA BOLETA EN LA BASE DE DATOS
                await repositorioTransacciones.Crear(transaccion);

                if (transaccion.Id > 0 && request.Detalles != null && request.Detalles.Any())
                {
                    await repositorioTransacciones.InsertarDetalles(transaccion.Id, request.Detalles);
                }

                return Ok();
            }
                
        }
}