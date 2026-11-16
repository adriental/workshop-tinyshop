using System.Net;
using System.Net.Http.Json;
using DataEntities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.Data;
using Products.Endpoints;

namespace TinyShop.Tests;

[TestClass]
public class ProductApiTests
{
    private WebApplication app = null!;
    private HttpClient client = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        var databaseName = Guid.NewGuid().ToString();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContext<ProductDataContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        app = builder.Build();
        app.MapProductEndpoints();
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    [TestMethod]
    public async Task GetProducts_ReturnsAllProducts()
    {
        var first = new Product { Name = "Flashlight", Price = 19.99m };
        var second = new Product { Name = "Hiking poles", Price = 24.99m };
        await AddProducts(first, second);

        var products = await client.GetFromJsonAsync<List<Product>>("/api/Product");

        Assert.IsNotNull(products);
        Assert.AreEqual(2, products.Count);
        Assert.AreSequenceEqual(
            [first.Name, second.Name], products.Select(product => product.Name).ToArray(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task GetProduct_ExistingId_ReturnsProduct()
    {
        var product = new Product { Name = "Flashlight", Price = 19.99m };
        await AddProducts(product);

        var response = await client.GetAsync($"/api/Product/{product.Id}");
        var returnedProduct = await response.Content.ReadFromJsonAsync<Product>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(returnedProduct);
        Assert.AreEqual(product.Id, returnedProduct.Id);
        Assert.AreEqual(product.Name, returnedProduct.Name);
        Assert.AreEqual(product.Price, returnedProduct.Price);
    }

    [TestMethod]
    public async Task GetProduct_UnknownId_ReturnsNotFound()
    {
        var response = await client.GetAsync("/api/Product/999");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateProduct_ReturnsCreatedProductAndLocation()
    {
        var product = new Product
        {
            Id = 987,
            Name = "Solar flashlight",
            Description = "Rechargeable outdoor light",
            Price = 19.99m,
            ImageUrl = "flashlight.png"
        };

        var response = await client.PostAsJsonAsync("/api/Product", product);
        var createdProduct = await response.Content.ReadFromJsonAsync<Product>();

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(createdProduct);
        Assert.AreNotEqual(987, createdProduct.Id);
        Assert.AreEqual(product.Name, createdProduct.Name);
        Assert.AreEqual(product.Description, createdProduct.Description);
        Assert.AreEqual(product.Price, createdProduct.Price);
        Assert.AreEqual(product.ImageUrl, createdProduct.ImageUrl);
        Assert.IsNotNull(response.Headers.Location);
        Assert.EndsWith($"/api/Product/{createdProduct.Id}", response.Headers.Location.ToString());

        var getResponse = await client.GetAsync(response.Headers.Location);
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [TestMethod]
    public async Task UpdateProduct_ExistingId_UpdatesProduct()
    {
        var product = new Product { Name = "Old name", Description = "Old description", Price = 1m };
        await AddProducts(product);
        var updatedProduct = new Product
        {
            Id = product.Id,
            Name = "New name",
            Description = "New description",
            Price = 2m,
            ImageUrl = "updated.png"
        };

        var response = await client.PutAsJsonAsync($"/api/Product/{product.Id}", updatedProduct);
        var storedProduct = await client.GetFromJsonAsync<Product>($"/api/Product/{product.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.IsNotNull(storedProduct);
        Assert.AreEqual(product.Id, storedProduct.Id);
        Assert.AreEqual("New name", storedProduct.Name);
        Assert.AreEqual("New description", storedProduct.Description);
        Assert.AreEqual(2m, storedProduct.Price);
        Assert.AreEqual("updated.png", storedProduct.ImageUrl);
    }

    [TestMethod]
    public async Task UpdateProduct_UnknownId_ReturnsNotFound()
    {
        var response = await client.PutAsJsonAsync("/api/Product/999", new Product { Name = "Missing" });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_ExistingId_DeletesProduct()
    {
        var product = new Product { Name = "Flashlight" };
        await AddProducts(product);

        var response = await client.DeleteAsync($"/api/Product/{product.Id}");
        var getResponse = await client.GetAsync($"/api/Product/{product.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [TestMethod]
    public async Task DeleteProduct_UnknownId_ReturnsNotFound()
    {
        var response = await client.DeleteAsync("/api/Product/999");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task AddProducts(params Product[] products)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDataContext>();
        db.Product.AddRange(products);
        await db.SaveChangesAsync();
    }
}
