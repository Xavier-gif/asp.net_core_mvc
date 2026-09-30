using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace ManejoPresupuesto.Controllers
{
    public class CategoriasController: Controller
    {
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IServiciosUsuarios serviciosUsuarios;
        public CategoriasController(IRepositorioCategorias repositorioCategorias, IServiciosUsuarios serviciosUsuarios)
        {
            this.repositorioCategorias = repositorioCategorias;
            this.serviciosUsuarios = serviciosUsuarios;
        }
        public async Task<IActionResult> Index(PaginacionViewModel paginacion)
        {
            var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
            var categorias = await repositorioCategorias.Obtener(usuarioId, paginacion);
            var totalCategorias = await repositorioCategorias.Contar(usuarioId);
            var repuestaVM= new PaginacionRespuesta<Categoria>
            {            
                    Elementos = categorias,
                    Pagina = paginacion.Pagina,
                    RecordsPorPagina = paginacion.RecordsPorPagina,
                    CantidadTotalRecords = totalCategorias,
                    BaseURL =Url.Action()
            };

            
            return View(repuestaVM);
        }
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Categoria categoria)
        {
             var usuarioId =serviciosUsuarios.ObtenerUsuarioId();

             if (!ModelState.IsValid)
            {
                return View(categoria); 
            }

            categoria.UsuarioId = usuarioId;
            await repositorioCategorias.Crear(categoria);
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
             var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
             var categoria = await repositorioCategorias.ObtenerPorId(id, usuarioId);

             if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            return View(categoria);
        }
        [HttpPost]
        public async Task<IActionResult> Editar(Categoria categoriaEditar)
        {
             if (!ModelState.IsValid)
            {
                return View(categoriaEditar); 
            }
              var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
             var categoria = await repositorioCategorias.ObtenerPorId(categoriaEditar.Id, usuarioId);

             if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            categoriaEditar.UsuarioId = usuarioId;

            await repositorioCategorias.Actualizar(categoriaEditar);

            return RedirectToAction("Index");

        }

        public async Task<IActionResult> Borrar(int id)
        {
              var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
              var categoria = await repositorioCategorias.ObtenerPorId(id, usuarioId);

             if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

              return View(categoria);
        }
        [HttpPost]
        public async Task<IActionResult> BorrarCategoria(int id)
        {
               var usuarioId =serviciosUsuarios.ObtenerUsuarioId();
              var categoria = await repositorioCategorias.ObtenerPorId(id, usuarioId);

             if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioCategorias.Borrar(id);
            return RedirectToAction("Index");
        }
    }
}