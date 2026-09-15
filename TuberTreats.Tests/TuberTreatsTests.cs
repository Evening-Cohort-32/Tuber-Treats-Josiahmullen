namespace TuberTreats.Tests;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using System.Net.Http.Json;
using TuberTreats.Models;

public class TuberTreatsTests
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public async void TestTuberOrders()
    {

        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        List<TuberOrder> orders = await Get<List<TuberOrder>>(client, "/tuberorders");
        List<TuberDriver> drivers = await Get<List<TuberDriver>>(client, "/tuberdrivers");
        List<Topping> toppings = await Get<List<Topping>>(client, "/toppings");
        List<TuberTopping> tuberToppings = await Get<List<TuberTopping>>(client, "/tubertoppings");
        List<Customer> customers = await Get<List<Customer>>(client, "/customers");

        Assert.True(orders.Any());
        Assert.True(orders[0].Id > 0);
        Assert.True(drivers.Any());
        Assert.True(drivers[0].Id > 0);
        Assert.True(toppings.Any());
        Assert.True(toppings[0].Id > 0);
        Assert.True(tuberToppings.Any());
        Assert.True(tuberToppings[0].Id > 0);
        Assert.True(customers.Any());
        Assert.True(customers[0].Id > 0);

        //single orders
        foreach (var order in orders)
        {
            var singleOrder = await Get<TuberOrder>(client, $"/tuberorders/{order.Id}");
            var orderToppings = tuberToppings
                .Where(tuberTopping => tuberTopping.TuberOrderId == singleOrder.Id)
                .Select(tuberTopping => toppings.First(topping => topping.Id == tuberTopping.ToppingId)).ToList();
            Assert.Equal(order.Id, singleOrder.Id);
            if (orderToppings.Count > 0)
            {
                Assert.True(Enumerable.SequenceEqual(orderToppings.OrderBy(t => t.Id), singleOrder.Toppings.OrderBy(t => t.Id), new ToppingComparer()));
            }
        }

        //make new order
        //get id of first customer
        var customerId = customers[0].Id;

        var newOrder = await Post<TuberOrder>(client, "/tuberorders", new TuberOrder
        {
            CustomerId = customerId
        });
        //new order gets a new id
        Assert.True(newOrder.Id != null && newOrder.Id > 0);
        // new order has the correct customer id
        Assert.Equal(customerId, newOrder.CustomerId);
        //new order has a OrderPlacedOnDate that is recent
        Assert.True(newOrder.OrderPlacedOnDate.AddHours(1) > DateTime.Now);
        //collection of orders is updated with new order
        var updatedOrders = await Get<List<TuberOrder>>(client, "/tuberorders");
        Assert.Equal(orders.Count + 1, updatedOrders.Count);

        //update order
        var employeeId = drivers[0].Id;
        newOrder.TuberDriverId = employeeId;
        var response = client.PutAsJsonAsync($"/tuberorders/{newOrder.Id}", newOrder);

        var updatedOrder = await Get<TuberOrder>(client, $"/tuberorders/{newOrder.Id}");

        Assert.Equal(employeeId, newOrder.TuberDriverId);

        //complete order
        await client.PostAsync($"/tuberorders/{newOrder.Id}/complete", null);
        var completedOrder = await Get<TuberOrder>(client, $"/tuberorders/{newOrder.Id}");
        //completed order has a DeliveredOnDate that is recent
        Assert.True(completedOrder.DeliveredOnDate?.AddHours(1) > DateTime.Now);
    }

    [Fact]
    public async void TestToppings()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        // we've already tested this endpoint
        List<Topping> toppings = await Get<List<Topping>>(client, "/toppings");
        var firstToppingId = toppings[0].Id;
        var topping = await Get<Topping>(client, $"/toppings/{firstToppingId}");
        Assert.Equal(firstToppingId, topping.Id);
        Assert.True(topping.Name != null);

    }

    [Fact]
    public async void TestTuberToppings()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        // we've already tested this endpoint
        List<TuberTopping> tuberToppings = await Get<List<TuberTopping>>(client, "/tubertoppings");

        //add new tuber topping
        List<TuberOrder> orders = await Get<List<TuberOrder>>(client, "/tuberorders");
        List<Topping> toppings = await Get<List<Topping>>(client, "/toppings");
        var firstOrder = await Get<TuberOrder>(client, $"/tuberorders/{orders[0].Id}");
        var firstTopping = toppings[0];
        var newTuberTopping = await Post<TuberTopping>(client, "/tubertoppings", new TuberTopping
        {
            TuberOrderId = firstOrder.Id,
            ToppingId = firstTopping.Id
        });

        //update our local order
        if (firstOrder.Toppings != null)
        {
            firstOrder.Toppings.Add(firstTopping);
        }
        else
        {
            firstOrder.Toppings = new List<Topping>() { firstTopping };
        }

        //new TuberTopping gets a new Id
        Assert.True(newTuberTopping.Id > 0);
        // new topping is added to the order
        var toppedOrder = await Get<TuberOrder>(client, $"/tuberorders/{firstOrder.Id}");

        //Check the local order and the order from the database for equality
        Assert.True(Enumerable.SequenceEqual(toppedOrder.Toppings.OrderBy(t => t.Id), firstOrder.Toppings.OrderBy(t => t.Id), new ToppingComparer()));

        //delete the new tubertopping
        await client.DeleteAsync($"/tubertoppings/{newTuberTopping.Id}");
        List<TuberTopping> tuberToppingsUpdated = await Get<List<TuberTopping>>(client, "/tubertoppings");
        //tuberToping should no longer be in the database. 
        Assert.True(!tuberToppingsUpdated.Any(tuberTopping => tuberTopping.Id == newTuberTopping.Id));
    }

    [Fact]
    public async void TestCustomers()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        List<TuberOrder> orders = await Get<List<TuberOrder>>(client, "/tuberorders");
        List<Customer> customers = await Get<List<Customer>>(client, "/customers");
        var lastCustomer = customers.LastOrDefault();
        var customerOrders = orders.Where(o => o.CustomerId == lastCustomer.Id);

        var lastCustomerWithOrders = await Get<Customer>(client, $"/customers/{lastCustomer.Id}");
        Assert.Equal(lastCustomerWithOrders.Name, lastCustomer.Name);
        Assert.True(Enumerable.SequenceEqual(lastCustomerWithOrders.TuberOrders.OrderBy(t => t.Id), customerOrders.OrderBy(t => t.Id), new OrderComparer()));

        //make a new customer
        var newCustomer = await Post<Customer>(client, "/customers", new Customer
        {
            Name = "Tony",
            Address = "101 Main Street"
        });
        //new customer gets a new id
        Assert.True(newCustomer.Id != null && newCustomer.Id > 0);

        //collection of customers is updated with new order
        var updatedCustomers = await Get<List<Customer>>(client, "/customers");
        Assert.Equal(customers.Count + 1, updatedCustomers.Count);

        //delete customer
        await client.DeleteAsync($"/customers/{newCustomer.Id}");
        var customersAfterDelete = await Get<List<Customer>>(client, "/customers");
        Assert.True(!customersAfterDelete.Any(c => c.Id == newCustomer.Id));
    }
    [Fact]
    public async void TestEmployees()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        List<TuberDriver> drivers = await Get<List<TuberDriver>>(client, "/tuberdrivers");
        List<TuberOrder> orders = await Get<List<TuberOrder>>(client, "/tuberorders");
        var firstDriver = drivers.FirstOrDefault();
        var driverOrders = orders.Where(o => o.TuberDriverId == firstDriver.Id);

        var firstDriverWithOrders = await Get<TuberDriver>(client, $"/tuberdrivers/{firstDriver.Id}");
        Assert.Equal(firstDriverWithOrders.Name, firstDriver.Name);
        Assert.True(Enumerable.SequenceEqual(firstDriverWithOrders.TuberDeliveries.OrderBy(t => t.Id), driverOrders.OrderBy(t => t.Id), new OrderComparer()));

    }

    private async Task<T> Get<T>(HttpClient client, string uri)
    {
        var response = await client.GetAsync(uri);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        return default;
    }

    private async Task<T> Post<T>(HttpClient client, string url, T obj)
    {
        var response = await client.PostAsJsonAsync(url, obj);
        var newObject = await response.Content.ReadFromJsonAsync<T>();
        return newObject;
    }

}

class ToppingComparer : IEqualityComparer<Topping>
{
    public bool Equals(Topping t1, Topping t2)
    {
        return t1.Id == t2.Id && t1.Name == t2.Name;
    }

    public int GetHashCode(Topping obj)
    {
        return base.GetHashCode();
    }
}
class OrderComparer : IEqualityComparer<TuberOrder>
{
    public bool Equals(TuberOrder t1, TuberOrder t2)
    {
        return t1.Id == t2.Id &&
               t1.TuberDriverId == t2.TuberDriverId &&
               t1.CustomerId == t2.CustomerId &&
               t1.OrderPlacedOnDate == t2.OrderPlacedOnDate &&
               t1.DeliveredOnDate == t2.DeliveredOnDate;
    }

    public int GetHashCode(TuberOrder obj)
    {
        return base.GetHashCode();
    }
}