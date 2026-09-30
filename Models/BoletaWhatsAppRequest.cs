namespace ManejoPresupuesto.Models
{
    public class BoletaWhatsAppRequest
    {
        public string Descripcion { get; set; }
        public decimal MontoTotal { get; set; }
        // ¡ESTA ES LA LÍNEA QUE FALTA PARA QUE DESAPAREZCA EL ERROR ROJO!
        public string CategoriaSugerida { get; set; }
        public List<DetalleBoleta> Detalles { get; set; }
    }

    public class DetalleBoleta
    {
        public string Producto { get; set; }
        public decimal Precio { get; set; }
    }
}