namespace ManejoPresupuesto.Models
{
    public class DetalleCartolaViewModel
    {
        public DateTime Fecha { get; set; }
        public string Comercio { get; set; } 
        public string Producto { get; set; }
        public decimal Precio { get; set; }
    }
}