using DataEntities;

namespace Store.Services;

public class ProductService(HttpClient httpClient)
{
    public async Task<List<Product>> GetProducts()
    {
        using var response = await httpClient.GetAsync("/api/Product");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.ListProduct)
            ?? throw new InvalidOperationException("The Products API returned an empty response.");
    }

    public async Task<Product?> GetProduct(int id)
    {
        using var response = await httpClient.GetAsync($"/api/Product/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.Product)
            ?? throw new InvalidOperationException("The Products API returned an empty response.");
    }

    public async Task<Product> CreateProduct(Product product)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/Product",
            product,
            ProductSerializerContext.Default.Product);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.Product)
            ?? throw new InvalidOperationException("The Products API returned an empty response.");
    }

    public async Task UpdateProduct(int id, Product product)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/Product/{id}",
            product,
            ProductSerializerContext.Default.Product);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProduct(int id)
    {
        using var response = await httpClient.DeleteAsync($"/api/Product/{id}");
        response.EnsureSuccessStatusCode();
    }
}