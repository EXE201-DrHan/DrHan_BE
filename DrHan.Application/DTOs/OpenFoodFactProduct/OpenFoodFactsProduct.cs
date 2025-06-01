using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.DTOs.OpenFoodFactProduct
{
    public class OpenFoodFactsProduct
    {
        public string? Code { get; set; }
        public string? ProductName { get; set; }
        public string? Brands { get; set; }
        public string? Categories { get; set; }
        public string? Ingredients { get; set; }
        public string? Allergens { get; set; }
        public string? Traces { get; set; }
        public string? Quantity { get; set; }
        public string? ServingSize { get; set; }
        public string? EnergyKcal100g { get; set; }
        public string? Manufacturer { get; set; }
        public string? ImageUrl { get; set; }
        public string? NutritionGrades { get; set; }
        public string? Countries { get; set; }
        public string? CreatedT { get; set; }
        public string? LastModifiedT { get; set; }
    }
}
