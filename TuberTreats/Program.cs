using TuberTreats.Models;

//  "Database" (in-memory seed data)

List<Customer> customers = new List<Customer>
{
    new Customer { Id = 1, Name = "Maggie Thompson", Address = "412 Elm Street" },
    new Customer { Id = 2, Name = "Darius Webb", Address = "88 Broadway Avenue" },
    new Customer { Id = 3, Name = "Priya Nair", Address = "1509 Charlotte Pike" },
    new Customer { Id = 4, Name = "Leon Castillo", Address = "27 Music Row" },
    new Customer { Id = 5, Name = "Hannah Osei", Address = "930 Gallatin Pike" }
};

List<TuberDriver> tuberDrivers = new List<TuberDriver>
{
    new TuberDriver { Id = 1, Name = "Carlos Mendez" },
    new TuberDriver { Id = 2, Name = "Tasha Greene" },
    new TuberDriver { Id = 3, Name = "Owen Fitzgerald" }
};

List<Topping> toppings = new List<Topping>
{
    new Topping { Id = 1, Name = "Butter" },
    new Topping { Id = 2, Name = "Sour Cream" },
    new Topping { Id = 3, Name = "Cheddar Cheese" },
    new Topping { Id = 4, Name = "Bacon Bits" },
    new Topping { Id = 5, Name = "Chives" }
};

List<TuberOrder> tuberOrders = new List<TuberOrder>
{
    // delivered order
    new TuberOrder
    {
        Id = 1,
        OrderPlacedOnDate = new DateTime(2026, 9, 20, 18, 15, 0),
        CustomerId = 1,
        TuberDriverId = 1,
        DeliveredOnDate = new DateTime(2026, 9, 20, 18, 52, 0)
    },
    // driver assigned, not delivered yet
    new TuberOrder
    {
        Id = 2,
        OrderPlacedOnDate = new DateTime(2026, 9, 25, 12, 5, 0),
        CustomerId = 2,
        TuberDriverId = 2,
        DeliveredOnDate = null
    },
    // no driver assigned yet
    new TuberOrder
    {
        Id = 3,
        OrderPlacedOnDate = new DateTime(2026, 9, 26, 19, 40, 0),
        CustomerId = 3,
        TuberDriverId = null,
        DeliveredOnDate = null
    },
    // another delivered order for customer 1 and driver 1
    new TuberOrder
    {
        Id = 4,
        OrderPlacedOnDate = new DateTime(2026, 9, 26, 20, 10, 0),
        CustomerId = 1,
        TuberDriverId = 1,
        DeliveredOnDate = new DateTime(2026, 9, 26, 20, 45, 0)
    }
};

List<TuberTopping> tuberToppings = new List<TuberTopping>
{
    new TuberTopping { Id = 1, TuberOrderId = 1, ToppingId = 1 },
    new TuberTopping { Id = 2, TuberOrderId = 1, ToppingId = 2 },
    new TuberTopping { Id = 3, TuberOrderId = 1, ToppingId = 3 },
    new TuberTopping { Id = 4, TuberOrderId = 2, ToppingId = 4 },
    new TuberTopping { Id = 5, TuberOrderId = 2, ToppingId = 5 },
    new TuberTopping { Id = 6, TuberOrderId = 4, ToppingId = 1 }
    // order 3 is a plain potato, no toppings
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

//add endpoints here

// TUBER ORDERS 

// get all orders
app.MapGet("/tuberorders", () =>
{
    return tuberOrders;
});

// get one order, with its customer, driver, and toppings
app.MapGet("/tuberorders/{id}", (int id) =>
{
    TuberOrder order = tuberOrders.FirstOrDefault(o => o.Id == id);
    if (order == null)
    {
        return Results.NotFound();
    }

    Customer customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);
    // if no driver is assigned, TuberDriverId is null and driver will also be null
    TuberDriver driver = tuberDrivers.FirstOrDefault(d => d.Id == order.TuberDriverId);

    // use the TuberToppings join table to find the toppings on this order
    List<Topping> orderToppings = tuberToppings
        .Where(tt => tt.TuberOrderId == order.Id)
        .Select(tt => toppings.FirstOrDefault(t => t.Id == tt.ToppingId))
        .ToList();

    // build a new object for the response so we don't change the order saved in our list
    return Results.Ok(new TuberOrder
    {
        Id = order.Id,
        OrderPlacedOnDate = order.OrderPlacedOnDate,
        CustomerId = order.CustomerId,
        Customer = customer,
        TuberDriverId = order.TuberDriverId,
        TuberDriver = driver,
        DeliveredOnDate = order.DeliveredOnDate,
        Toppings = orderToppings
    });
});

// submit a new order (the API sets the Id and OrderPlacedOnDate)
app.MapPost("/tuberorders", (TuberOrder order) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);
    if (customer == null)
    {
        return Results.BadRequest("That customer does not exist.");
    }

    TuberOrder newOrder = new TuberOrder
    {
        Id = tuberOrders.Count > 0 ? tuberOrders.Max(o => o.Id) + 1 : 1,
        CustomerId = order.CustomerId,
        OrderPlacedOnDate = DateTime.Now
    };
    tuberOrders.Add(newOrder);

    return Results.Created($"/tuberorders/{newOrder.Id}", newOrder);
});

// assign a driver to an order
app.MapPut("/tuberorders/{id}", (int id, TuberOrder order) =>
{
    TuberOrder orderToUpdate = tuberOrders.FirstOrDefault(o => o.Id == id);
    if (orderToUpdate == null)
    {
        return Results.NotFound();
    }
    if (id != order.Id)
    {
        return Results.BadRequest("The id in the url does not match the order id.");
    }

    TuberDriver driver = tuberDrivers.FirstOrDefault(d => d.Id == order.TuberDriverId);
    if (driver == null)
    {
        return Results.BadRequest("That driver does not exist.");
    }

    orderToUpdate.TuberDriverId = order.TuberDriverId;

    return Results.NoContent();
});

// complete (deliver) an order
app.MapPost("/tuberorders/{id}/complete", (int id) =>
{
    TuberOrder order = tuberOrders.FirstOrDefault(o => o.Id == id);
    if (order == null)
    {
        return Results.NotFound();
    }

    order.DeliveredOnDate = DateTime.Now;

    return Results.NoContent();
});

// ---------- TOPPINGS ----------

// get all toppings
app.MapGet("/toppings", () =>
{
    return toppings;
});

// get one topping
app.MapGet("/toppings/{id}", (int id) =>
{
    Topping topping = toppings.FirstOrDefault(t => t.Id == id);
    if (topping == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(topping);
});

//TUBER TOPPINGS yum

// get all tuber toppings
app.MapGet("/tubertoppings", () =>
{
    return tuberToppings;
});

// add a topping to an order
app.MapPost("/tubertoppings", (TuberTopping tuberTopping) =>
{
    TuberOrder order = tuberOrders.FirstOrDefault(o => o.Id == tuberTopping.TuberOrderId);
    Topping topping = toppings.FirstOrDefault(t => t.Id == tuberTopping.ToppingId);
    if (order == null || topping == null)
    {
        return Results.BadRequest("That order or topping does not exist.");
    }

    tuberTopping.Id = tuberToppings.Count > 0 ? tuberToppings.Max(tt => tt.Id) + 1 : 1;
    tuberToppings.Add(tuberTopping);

    return Results.Created($"/tubertoppings/{tuberTopping.Id}", tuberTopping);
});

// remove a topping from an order
app.MapDelete("/tubertoppings/{id}", (int id) =>
{
    TuberTopping tuberTopping = tuberToppings.FirstOrDefault(tt => tt.Id == id);
    if (tuberTopping == null)
    {
        return Results.NotFound();
    }

    tuberToppings.Remove(tuberTopping);

    return Results.NoContent();
});

//  CUSTOMERS 

// get all customers
app.MapGet("/customers", () =>
{
    return customers;
});

// get one customer, with their orders
app.MapGet("/customers/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new Customer
    {
        Id = customer.Id,
        Name = customer.Name,
        Address = customer.Address,
        TuberOrders = tuberOrders.Where(o => o.CustomerId == customer.Id).ToList()
    });
});

// add a customer
app.MapPost("/customers", (Customer customer) =>
{
    if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Address))
    {
        return Results.BadRequest("A customer needs a name and an address.");
    }

    Customer newCustomer = new Customer
    {
        Id = customers.Count > 0 ? customers.Max(c => c.Id) + 1 : 1,
        Name = customer.Name,
        Address = customer.Address
    };
    customers.Add(newCustomer);

    return Results.Created($"/customers/{newCustomer.Id}", newCustomer);
});

// delete a customer
app.MapDelete("/customers/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }

    // also removes this customer's orders 
    // so we don't have orders pointing to a customer that doesn't exist
    List<int> customerOrderIds = tuberOrders
        .Where(o => o.CustomerId == id)
        .Select(o => o.Id)
        .ToList();
    tuberToppings.RemoveAll(tt => customerOrderIds.Contains(tt.TuberOrderId));
    tuberOrders.RemoveAll(o => o.CustomerId == id);

    customers.Remove(customer);

    return Results.NoContent();
});

//  TUBER DRIVERS 

// get all drivers 
app.MapGet("/tuberdrivers", () =>
{
    return tuberDrivers;
});

// get one driver, with their deliveries
app.MapGet("/tuberdrivers/{id}", (int id) =>
{
    TuberDriver driver = tuberDrivers.FirstOrDefault(d => d.Id == id);
    if (driver == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new TuberDriver
    {
        Id = driver.Id,
        Name = driver.Name,
        TuberDeliveries = tuberOrders.Where(o => o.TuberDriverId == driver.Id).ToList()
    });
});

app.Run();
//don't touch or move this!
public partial class Program { }
