using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dairy.Infrastructure;
using Dapper;

public class DatabaseSetupModel : PageModel
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DatabaseSetupModel(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public string Message { get; set; } = "";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            // Create schema
            await connection.ExecuteAsync("CREATE SCHEMA IF NOT EXISTS dairy");
            await connection.ExecuteAsync("SET search_path TO dairy");
            
            // Create inventory tables
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.inventory_items (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    unit VARCHAR(20) NOT NULL DEFAULT 'Liters',
                    min_stock DECIMAL(10,2) DEFAULT 0,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.inventory_transactions (
                    id SERIAL PRIMARY KEY,
                    item_id INT NOT NULL REFERENCES dairy.inventory_items(id),
                    transaction_type VARCHAR(10) NOT NULL CHECK (transaction_type IN ('IN', 'OUT')),
                    quantity DECIMAL(10,2) NOT NULL CHECK (quantity > 0),
                    reference VARCHAR(255),
                    transaction_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )");

            // Create routes tables
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.routes (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    driver_name VARCHAR(100) NOT NULL,
                    vehicle_number VARCHAR(20) NOT NULL,
                    status VARCHAR(20) DEFAULT 'Active',
                    total_distance DECIMAL(8,2) DEFAULT 0,
                    started_at TIMESTAMP WITH TIME ZONE,
                    completed_at TIMESTAMP WITH TIME ZONE,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.route_farmers (
                    id SERIAL PRIMARY KEY,
                    route_id INT NOT NULL REFERENCES dairy.routes(id) ON DELETE CASCADE,
                    farmer_id INT NOT NULL REFERENCES dairy.farmer(id) ON DELETE CASCADE,
                    sequence_order INT NOT NULL,
                    estimated_time TIME,
                    UNIQUE(route_id, farmer_id)
                )");

            // Create subscription tables
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.products (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    unit VARCHAR(20) DEFAULT 'Liters',
                    price DECIMAL(8,2) NOT NULL,
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.subscriptions (
                    id SERIAL PRIMARY KEY,
                    customer_id INT NOT NULL REFERENCES dairy.customer(id),
                    product_id INT NOT NULL REFERENCES dairy.products(id),
                    quantity DECIMAL(8,2) NOT NULL CHECK (quantity > 0),
                    frequency VARCHAR(20) NOT NULL CHECK (frequency IN ('Daily', 'Weekly', 'Monthly')),
                    start_date DATE NOT NULL,
                    next_delivery_date DATE NOT NULL,
                    end_date DATE,
                    status VARCHAR(20) DEFAULT 'Active',
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS dairy.subscription_deliveries (
                    id SERIAL PRIMARY KEY,
                    subscription_id INT NOT NULL REFERENCES dairy.subscriptions(id),
                    delivery_date DATE NOT NULL,
                    delivered_quantity DECIMAL(8,2),
                    status VARCHAR(20) DEFAULT 'Scheduled',
                    notes TEXT,
                    delivered_by INT REFERENCES dairy.employee(id),
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )");

            // Insert sample data
            await connection.ExecuteAsync(@"
                INSERT INTO inventory_items (name, unit, min_stock) VALUES
                ('Milk', 'Liters', 100.0),
                ('Paneer', 'Kg', 10.0),
                ('Ghee', 'Kg', 5.0),
                ('Butter', 'Kg', 8.0),
                ('Curd', 'Liters', 50.0)
                ON CONFLICT DO NOTHING;
                
                INSERT INTO inventory_transactions (item_id, transaction_type, quantity, reference) VALUES
                (1, 'IN', 500.0, 'Initial Stock'),
                (2, 'IN', 25.0, 'Production'),
                (3, 'IN', 15.0, 'Production')
                ON CONFLICT DO NOTHING;
                
                INSERT INTO products (name, unit, price) VALUES
                ('Fresh Milk', 'Liters', 55.00),
                ('Toned Milk', 'Liters', 50.00),
                ('Full Cream Milk', 'Liters', 60.00),
                ('Paneer', 'Kg', 350.00),
                ('Curd', 'Liters', 45.00)
                ON CONFLICT DO NOTHING;
                
                INSERT INTO routes (name, driver_name, vehicle_number, total_distance) VALUES
                ('Route A - North', 'Ramesh Kumar', 'MH12AB1234', 25.5),
                ('Route B - South', 'Suresh Patil', 'MH12CD5678', 18.2),
                ('Route C - East', 'Mahesh Singh', 'MH12EF9012', 32.1)
                ON CONFLICT DO NOTHING");

            await connection.ExecuteAsync(@"
                INSERT INTO dairy.products (name, unit, price) VALUES
                ('Fresh Milk', 'Liters', 55.00),
                ('Toned Milk', 'Liters', 50.00),
                ('Paneer', 'Kg', 350.00)
                ON CONFLICT DO NOTHING");

            await connection.ExecuteAsync(@"
                INSERT INTO dairy.routes (name, driver_name, vehicle_number, total_distance) VALUES
                ('Route A - North', 'Ramesh Kumar', 'MH12AB1234', 25.5),
                ('Route B - South', 'Suresh Patil', 'MH12CD5678', 18.2)
                ON CONFLICT DO NOTHING");

            // Create additional tables for new features
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS farmer_loans (
                    id SERIAL PRIMARY KEY,
                    farmer_id INT NOT NULL REFERENCES farmer(id),
                    loan_type VARCHAR(20) NOT NULL,
                    amount DECIMAL(12,2) NOT NULL,
                    outstanding_amount DECIMAL(12,2) NOT NULL,
                    due_date DATE NOT NULL,
                    interest_rate DECIMAL(5,2) DEFAULT 0,
                    status VARCHAR(20) DEFAULT 'Active',
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );
                
                CREATE TABLE IF NOT EXISTS loan_payments (
                    id SERIAL PRIMARY KEY,
                    loan_id INT NOT NULL REFERENCES farmer_loans(id),
                    amount DECIMAL(12,2) NOT NULL,
                    payment_date DATE NOT NULL,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );
                
                CREATE TABLE IF NOT EXISTS invoices (
                    id SERIAL PRIMARY KEY,
                    invoice_number VARCHAR(30) UNIQUE NOT NULL,
                    customer_id INT REFERENCES customer(id),
                    invoice_date DATE NOT NULL,
                    subtotal DECIMAL(12,2) NOT NULL,
                    tax_amount DECIMAL(12,2) DEFAULT 0,
                    total_amount DECIMAL(12,2) NOT NULL,
                    payment_method VARCHAR(20) NOT NULL,
                    status VARCHAR(20) DEFAULT 'Pending',
                    paid_date DATE,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                );
                
                CREATE TABLE IF NOT EXISTS invoice_items (
                    id SERIAL PRIMARY KEY,
                    invoice_id INT NOT NULL REFERENCES invoices(id),
                    product_id INT NOT NULL REFERENCES products(id),
                    quantity DECIMAL(8,2) NOT NULL,
                    unit_price DECIMAL(8,2) NOT NULL,
                    total_price DECIMAL(12,2) NOT NULL
                )");

            Message = "All database tables created successfully with sample data!";
        }
        catch (Exception ex)
        {
            Message = $"Error: {ex.Message}";
        }

        return Page();
    }
}