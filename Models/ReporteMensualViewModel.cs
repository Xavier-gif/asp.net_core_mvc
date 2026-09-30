using Microsoft.VisualBasic;

namespace ManejoPresupuesto.Models
{
    public class ReporteMensualViewModel
    {
        public IEnumerable<ResultadoObtenerPorMes> TransaccionesPorMes { get; set; }
        public decimal Ingresos => TransaccionesPorMes.Sum(x => x.Ingreso);
        public decimal Gasto => TransaccionesPorMes.Sum(x => x.Gasto); 
        public decimal Total => Ingresos - Gasto;
        public int Año { get; set; }
    }
}