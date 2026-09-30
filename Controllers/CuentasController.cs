using System.Collections;
using System.Runtime.InteropServices;
using AutoMapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace ManejoPresupuesto.Controllers
{
    public class CuentasController: Controller
    {
        private readonly IRepositorioTiposCuentas repositorioTiposCuentas;
        private readonly IServiciosUsuarios serviciosUsuarios;
        private readonly IRepositorioCuenta repositorioCuenta;
        private readonly IMapper mapper;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IServiciosReportes serviciosReportes;

        public CuentasController(IRepositorioTiposCuentas repositorioTiposCuentas, IServiciosUsuarios serviciosUsuarios, 
                                 IRepositorioCuenta repositorioCuenta, IMapper mapper, IRepositorioTransacciones repositorioTransacciones,
                                 IServiciosReportes serviciosReportes)
        {
            this.repositorioTiposCuentas = repositorioTiposCuentas;
            this.serviciosUsuarios = serviciosUsuarios;
            this.repositorioCuenta = repositorioCuenta;
            this.repositorioCuenta = repositorioCuenta;
            this.mapper= mapper;
            this.repositorioTransacciones = repositorioTransacciones;
            this.serviciosReportes = serviciosReportes;
        }

     public async Task<IActionResult> Index()
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            
            // 1. Obtenemos las cuentas y los tipos de cuentas
            var cuentaConTipoCuenta = await repositorioCuenta.Buscar(usuarioId);
            var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);

            // 2. Filtramos los nombres de los tipos de cuenta que marcaste como "Pasivos"
            var nombresTiposPasivos = tiposCuentas.Where(t => t.EsPasivo).Select(t => t.Nombre).ToList();

            // 3. Calculamos la matemática
            decimal totalActivos = 0;
            decimal totalPasivos = 0;

            foreach (var cuenta in cuentaConTipoCuenta)
            {
                // Si el tipo de la cuenta existe en nuestra lista de pasivos, se suma a pasivos
                if (nombresTiposPasivos.Contains(cuenta.TipoCuenta))
                {
                    totalPasivos += cuenta.Balance;
                }
                else
                {
                    totalActivos += cuenta.Balance;
                }
            }

            // 4. Agrupamos para la vista original
            var modelo = cuentaConTipoCuenta.GroupBy(x => x.TipoCuenta).
                Select(grupo => new IndiceCuentaViewModel
                {
                     TipoCuenta = grupo.Key,
                     Cuentas = grupo.AsEnumerable()
                }).ToList();

            // 5. Enviamos los totales calculados a la vista mediante ViewBag
            ViewBag.TotalActivos = totalActivos;
            ViewBag.TotalPasivos = totalPasivos;
            ViewBag.Total = totalActivos - totalPasivos;

            return View(modelo);
        }

        public async Task<IActionResult> Detalle(int id, int mes, int año)
        {
           var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
           var cuenta = await repositorioCuenta.obtenerPorId(id, usuarioId);

           if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

           var modelo = await serviciosReportes.ObtenerReporteTransaccionesDetalladasPorCuenta(usuarioId, id, mes, año, ViewBag);


           ViewBag.Cuenta = cuenta.Nombre;

           
            
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
           
            var modelo = new CuentaCreacionViewModel();

            modelo.TiposCuentas = await obtenerTiposCuentas(usuarioId);
            return View(modelo);
        }

        public async Task<IActionResult> Crear(CuentaCreacionViewModel cuenta)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(cuenta.TipoCuentaId, usuarioId);

            if (tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            if (!ModelState.IsValid)
            {
                cuenta.TiposCuentas = await obtenerTiposCuentas(usuarioId);
                return View(cuenta);
            }

            await repositorioCuenta.Crear(cuenta);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId= serviciosUsuarios.ObtenerUsuarioId();
            var cuenta = await repositorioCuenta.obtenerPorId(id,usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = mapper.Map<CuentaCreacionViewModel>(cuenta);

            modelo.TiposCuentas = await obtenerTiposCuentas(usuarioId);

            return View(modelo);
        }

        [HttpPost]

        public async Task<IActionResult> Editar(CuentaCreacionViewModel cuentaEditar)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var cuenta = await repositorioCuenta.obtenerPorId(cuentaEditar.Id, usuarioId);

            if(cuenta is null)
            {
                 return RedirectToAction("NoEncontrado", "Home");
            }

            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(cuentaEditar.TipoCuentaId, usuarioId);

            if(tipoCuenta is null)
            {
                 return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioCuenta.Actualizar(cuentaEditar);
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Borrar(int id)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
            var cuenta= await repositorioCuenta.obtenerPorId(id, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            return View(cuenta);
        }
        [HttpPost]
        public async Task<IActionResult> BorrarCuenta(int id)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
            var cuenta= await repositorioCuenta.obtenerPorId(id, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            await repositorioCuenta.Borrar(id);
            return RedirectToAction("Index");
        }
        private async Task<IEnumerable<SelectListItem>> obtenerTiposCuentas(int usuarioId)
        {
            var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);
            return tiposCuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }
    }
}