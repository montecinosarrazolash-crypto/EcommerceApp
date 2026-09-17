namespace EcommerceApp.Models
{
    public class ProductCatalogViewModel
    {
        public string? SelectedCategory { get; set; }
        public List<CategorySectionViewModel> Sections { get; set; } = new();
    }

    public class CategorySectionViewModel
    {
        public string Category { get; set; } = string.Empty;
        public List<Product> Products { get; set; } = new();
    }
}
