CREATE SCHEMA IF NOT EXISTS dairy;
SET search_path TO dairy;

CREATE TABLE IF NOT EXISTS audit_log (
    id SERIAL PRIMARY KEY,
    user_id INTEGER,
    action VARCHAR(100) NOT NULL,
    entity VARCHAR(50),
    entity_id INTEGER,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    details TEXT
);

CREATE TABLE IF NOT EXISTS bank (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS branch (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    address TEXT,
    contact VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS customer (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    contact VARCHAR(50) NOT NULL,
    branch_id INTEGER REFERENCES branch(id)
);

CREATE TABLE IF NOT EXISTS employee (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    contact VARCHAR(50) NOT NULL,
    branch_id INTEGER REFERENCES branch(id),
    role VARCHAR(30) NOT NULL
);

CREATE TABLE IF NOT EXISTS expense (
    id SERIAL PRIMARY KEY,
    category_id INTEGER REFERENCES expense_category(id),
    branch_id INTEGER REFERENCES branch(id),
    amount NUMERIC(12,2) NOT NULL,
    date DATE NOT NULL,
    notes TEXT,
    created_by INTEGER REFERENCES employee(id)
);

CREATE TABLE IF NOT EXISTS expense_category (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS farmer (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) UNIQUE NOT NULL,
    contact VARCHAR(50) NOT NULL,
    bank_id INTEGER REFERENCES bank(id),
    branch_id INTEGER REFERENCES branch(id)
);

CREATE TABLE IF NOT EXISTS item (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS milk_collection (
    id SERIAL PRIMARY KEY,
    farmer_id INTEGER REFERENCES farmer(id),
    shift_id INTEGER REFERENCES shift(id),
    date DATE NOT NULL,
    qty_ltr NUMERIC(8,2) NOT NULL CHECK (qty_ltr > 0 AND qty_ltr <= 999),
    fat_pct NUMERIC(4,2) NOT NULL CHECK (fat_pct >= 0 AND fat_pct <= 15),
    price_per_ltr NUMERIC(8,2) NOT NULL,
    due_amt NUMERIC(12,2) NOT NULL,
    notes TEXT,
    created_by INTEGER REFERENCES employee(id),
    UNIQUE (farmer_id, shift_id, date)
);

CREATE TABLE IF NOT EXISTS payment_customer (
    id SERIAL PRIMARY KEY,
    customer_id INTEGER REFERENCES customer(id),
    sale_id INTEGER REFERENCES sale(id),
    amount NUMERIC(12,2) NOT NULL,
    date DATE NOT NULL,
    invoice_no VARCHAR(30) UNIQUE,
    pdf_path VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS payment_farmer (
    id SERIAL PRIMARY KEY,
    farmer_id INTEGER REFERENCES farmer(id),
    milk_collection_id INTEGER REFERENCES milk_collection(id),
    amount NUMERIC(12,2) NOT NULL,
    date DATE NOT NULL,
    invoice_no VARCHAR(30) UNIQUE,
    pdf_path VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS product_conversion (
    id SERIAL PRIMARY KEY,
    item_id INTEGER REFERENCES item(id),
    milk_qty NUMERIC(8,2) NOT NULL,
    product_qty NUMERIC(8,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS rate_chart (
    id SERIAL PRIMARY KEY,
    milk_type VARCHAR(30) NOT NULL,
    shift_id INTEGER REFERENCES shift(id),
    rate NUMERIC(8,2) NOT NULL,
    effective_from DATE NOT NULL,
    effective_to DATE,
    file_ref VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS sale (
    id SERIAL PRIMARY KEY,
    customer_id INTEGER REFERENCES customer(id),
    shift_id INTEGER REFERENCES shift(id),
    date DATE NOT NULL,
    qty_ltr NUMERIC(8,2) NOT NULL CHECK (qty_ltr > 0 AND qty_ltr <= 999),
    unit_price NUMERIC(8,2) NOT NULL,
    discount NUMERIC(8,2) DEFAULT 0,
    paid_amt NUMERIC(12,2) NOT NULL,
    due_amt NUMERIC(12,2) NOT NULL,
    created_by INTEGER REFERENCES employee(id)
);

CREATE TABLE IF NOT EXISTS sell_rate (
    id SERIAL PRIMARY KEY,
    item_id INTEGER,
    rate NUMERIC(8,2) NOT NULL,
    effective_from DATE NOT NULL,
    effective_to DATE,
    branch_id INTEGER REFERENCES branch(id)
);

CREATE TABLE IF NOT EXISTS settings (
    id SERIAL PRIMARY KEY,
    system_name VARCHAR(100) NOT NULL,
    contact VARCHAR(50) NOT NULL,
    address TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS shift (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    start_time TIME,
    end_time TIME
);

CREATE TABLE IF NOT EXISTS user_account (
    id SERIAL PRIMARY KEY,
    login VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(30) NOT NULL,
    person_ref INTEGER REFERENCES employee(id)
);

CREATE INDEX IF NOT EXISTS idx_milk_collection_date_shift ON milk_collection(date, shift_id);
CREATE INDEX IF NOT EXISTS idx_milk_collection_farmer_date ON milk_collection(farmer_id, date);
CREATE INDEX IF NOT EXISTS idx_sale_date_shift ON sale(date, shift_id);
CREATE INDEX IF NOT EXISTS idx_sale_customer_date ON sale(customer_id, date);

INSERT INTO branch (name, address, contact) VALUES ('Main Branch', '123 Dairy Lane', '9876543210') ON CONFLICT DO NOTHING;
INSERT INTO bank (name) VALUES ('State Bank of India') ON CONFLICT DO NOTHING;
INSERT INTO employee (name, contact, branch_id, role) VALUES ('Admin User', '9999999999', 1, 'Admin') ON CONFLICT DO NOTHING;
INSERT INTO farmer (name, code, contact, bank_id, branch_id) VALUES ('Farmer A', 'F001', '7777777777', 1, 1) ON CONFLICT (code) DO NOTHING;
INSERT INTO customer (name, contact, branch_id) VALUES ('Customer X', '5555555555', 1) ON CONFLICT DO NOTHING;
INSERT INTO shift (name, start_time, end_time) VALUES ('Morning', '06:00', '10:00') ON CONFLICT DO NOTHING;
INSERT INTO expense_category (name) VALUES ('Transport') ON CONFLICT DO NOTHING;
INSERT INTO item (name) VALUES ('Milk') ON CONFLICT DO NOTHING;
INSERT INTO settings (system_name, contact, address) VALUES ('Dairy Management System', '9876543210', '123 Dairy Lane') ON CONFLICT DO NOTHING;