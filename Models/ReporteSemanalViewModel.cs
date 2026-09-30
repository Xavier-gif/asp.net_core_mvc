namespace ManejoPresupuesto.Models
{
    public class ReporteSemanalViewModel
    {
        public decimal Ingreso => TransaccionesPorSemana.Sum(x => x.ingreso);
        public decimal Gastos  => TransaccionesPorSemana.Sum(x => x.gastos);
        public decimal Total => Ingreso - Gastos;
        public DateTime FechaReferencia { get; set; }
        public IEnumerable<ResultadoObtenerPorSemana> TransaccionesPorSemana { get; set; }
    }
}