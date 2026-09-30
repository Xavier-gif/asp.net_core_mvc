namespace ManejoPresupuesto.Models
{
    public class TransaccionActualizacionViewModel: TransaccionCreacionViewModel
    {
        public int cuentaAnteriorId { get; set; }
        public decimal montoAnterior { get; set; }
        public string urlRetorno { get; set; }
    }
}