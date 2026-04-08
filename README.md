# RabbitMQ With 3 .NET 9 Microservices (Products, Carts, Payments)

This sample implements an event-driven flow using **RabbitMQ** between three microservices:

- **Products API**
  - Stores products in a static in-memory collection.
  - Publishes `product.created` when a product is created.
- **Carts API**
  - Consumes `product.created` and keeps a local product catalog cache.
  - Stores carts in a static in-memory collection.
  - Publishes `cart.checkout` when a cart is checked out.
- **Payments API**
  - Consumes `cart.checkout`.
  - Stores approved payments in a static in-memory collection.

## Solution structure

```text
src/
├── ProducerApi/   (existing sample)
├── ConsumerApi/   (existing sample)
├── ProductsApi/
├── CartsApi/
└── PaymentsApi/
```

## Event flow

1. `POST /products` on Products API.
2. Products API saves product and publishes `product.created`.
3. Carts API consumer receives `product.created` and updates local product catalog.
4. `POST /carts/{cartId}/items` on Carts API adds cart items using that catalog.
5. `POST /carts/{cartId}/checkout` publishes `cart.checkout`.
6. Payments API consumer receives `cart.checkout` and stores an approved payment record.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or equivalent Docker runtime
- `curl` (optional, for quick testing)

## 1) Start RabbitMQ

```bash
docker compose up -d
```

RabbitMQ endpoints:

- AMQP: `localhost:5672`
- Management UI: `http://localhost:15672`
- Username: `guest`
- Password: `guest`

## 2) Run all three microservices

Open **three terminals** from repository root.

### Terminal A: Products API

```bash
dotnet run --project src/ProductsApi/ProductsApi.csproj
```

### Terminal B: Carts API

```bash
dotnet run --project src/CartsApi/CartsApi.csproj
```

### Terminal C: Payments API

```bash
dotnet run --project src/PaymentsApi/PaymentsApi.csproj
```

> Port values are assigned by ASP.NET at runtime unless launch profiles force specific ports. Use each terminal log or Swagger URL output.

---

## 3) End-to-end test scenario

Use the following sequence to test the full asynchronous message exchange.

For examples below, assume:

- Products API = `http://localhost:5001`
- Carts API = `http://localhost:5002`
- Payments API = `http://localhost:5003`

Adjust to your actual ports.

### Step 1: Create a product (Products API)

```bash
curl -X POST http://localhost:5001/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Wireless Mouse",
    "price": 49.99
  }'
```

Save the returned `id` as `PRODUCT_ID`.

### Step 2: Confirm Carts API received product via RabbitMQ

```bash
curl http://localhost:5002/catalog
```

You should see the product from Step 1.

### Step 3: Add item to cart (Carts API)

Choose any GUID for `CART_ID`, for example:

```text
11111111-1111-1111-1111-111111111111
```

```bash
curl -X POST http://localhost:5002/carts/11111111-1111-1111-1111-111111111111/items \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "PRODUCT_ID",
    "quantity": 2
  }'
```

Replace `PRODUCT_ID` with the real GUID string.

### Step 4: Checkout cart (Carts API publishes `cart.checkout`)

```bash
curl -X POST http://localhost:5002/carts/11111111-1111-1111-1111-111111111111/checkout \
  -H "Content-Type: application/json" \
  -d '{
    "cardHolder": "John Doe",
    "cardNumber": "4111111111111111",
    "currency": "USD"
  }'
```

### Step 5: Verify payment processed (Payments API)

```bash
curl http://localhost:5003/payments
```

You should see a payment record with status `Approved`.

---

## API endpoints

### Products API

- `GET /` - service info
- `GET /products` - list in-memory products
- `POST /products` - create product + publish `product.created`
- Swagger: `/swagger`

### Carts API

- `GET /` - service info
- `GET /catalog` - products known by cart service via RabbitMQ
- `POST /carts/{cartId}/items` - add item to cart
- `GET /carts/{cartId}` - read cart
- `POST /carts/{cartId}/checkout` - publish `cart.checkout` and clear cart
- Swagger: `/swagger`

### Payments API

- `GET /` - service info
- `GET /payments` - list in-memory payment records
- Swagger: `/swagger`

---

## Notes

- All data storage is static in-memory for demo/testing purposes only.
- Restarting a service clears that service's static data.
- RabbitMQ exchange/queues are created automatically by the services.
- Existing `ProducerApi` and `ConsumerApi` projects are left unchanged as legacy sample components.
