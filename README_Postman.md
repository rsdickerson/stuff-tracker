# Using Postman with StuffTracker GraphQL API

This guide demonstrates how to use Postman to interact with the StuffTracker GraphQL API.

## Prerequisites

1. **Postman installed** - Download from [postman.com](https://www.postman.com/downloads/)
2. **API running** - Start the API using `dotnet run` from the `StuffTracker.Api` directory
3. **Database running** - Ensure MySQL is running via Docker Compose (`docker-compose up -d`)

## Postman Setup

### Create a New Request

1. Open Postman
2. Create a new HTTP request
3. Set the **Method** to `POST`
4. Set the **URL** to:
   - `http://localhost:5030/graphql` (HTTP)
   - OR `https://localhost:7185/graphql` (HTTPS)

### Configure Headers

Add the following headers:

| Key | Value |
|-----|-------|
| `Content-Type` | `application/json` |

### Request Body Format

Select **Body** → **raw** → **JSON**, and use this structure:

```json
{
  "query": "your GraphQL query or mutation here"
}
```

For queries with variables:

```json
{
  "query": "your GraphQL query",
  "variables": {
    "variableName": "variableValue"
  }
}
```

---

## Queries

### 1. Get All Locations (Paginated)

**Request Body:**
```json
{
  "query": "query { locations(first: 10) { nodes { id name createdAt } pageInfo { hasNextPage hasPreviousPage startCursor endCursor } } }"
}
```

**Response Fields:**
- `nodes`: Array of locations
- `pageInfo`: Pagination information

### 2. Get a Single Location by ID

**Request Body:**
```json
{
  "query": "query { location(id: 1) { id name createdAt } }"
}
```

### 3. Get All Items (Paginated)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10) { nodes { id name quantity roomId createdAt } pageInfo { hasNextPage hasPreviousPage } } }"
}
```

**Note:** The `search` parameter filters items by name (case-insensitive). Use `null` to get all items.

### 4. Search Items by Name

**Request Body:**
```json
{
  "query": "query { items(search: \"laptop\", first: 10) { nodes { id name quantity roomId createdAt } } }"
}
```

This searches for items whose names contain "laptop" (case-insensitive).

---

## Filtering

Hot Chocolate provides advanced filtering capabilities. Use the `where` argument with filter operators.

### Filter Items by Name (Contains)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, where: { name: { contains: \"laptop\" } }) { nodes { id name quantity roomId } } }"
}
```

### Filter Items by Quantity

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, where: { quantity: { gt: 5 } }) { nodes { id name quantity roomId } } }"
}
```

**Available quantity filters:**
- `gt`: greater than
- `gte`: greater than or equal
- `lt`: less than
- `lte`: less than or equal
- `eq`: equals
- `neq`: not equals

### Filter Items by Room ID

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, where: { roomId: { eq: 1 } }) { nodes { id name quantity roomId } } }"
}
```

### Filter Locations by Name

**Request Body:**
```json
{
  "query": "query { locations(first: 10, where: { name: { contains: \"Home\" } }) { nodes { id name createdAt } } }"
}
```

---

## Sorting

Use the `order` argument with field names and direction (`ASC` or `DESC`).

### Sort Items by Name (Ascending)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, order: [{ name: ASC }]) { nodes { id name quantity } } }"
}
```

### Sort Items by Name (Descending)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, order: [{ name: DESC }]) { nodes { id name quantity } } }"
}
```

### Sort Items by Quantity (Descending)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, order: [{ quantity: DESC }]) { nodes { id name quantity } } }"
}
```

### Sort Locations by Name

**Request Body:**
```json
{
  "query": "query { locations(first: 10, order: [{ name: ASC }]) { nodes { id name } } }"
}
```

### Multiple Sort Fields

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, order: [{ quantity: DESC }, { name: ASC }]) { nodes { id name quantity } } }"
}
```

This sorts by quantity (descending), then by name (ascending) for items with the same quantity.

---

## Pagination

The API uses cursor-based pagination. Use `first` and `after` for forward pagination, or `last` and `before` for backward pagination.

### Get First Page (First 10 Items)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10) { nodes { id name quantity } pageInfo { hasNextPage hasPreviousPage endCursor } } }"
}
```

### Get Next Page (Using Cursor)

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, after: \"YOUR_CURSOR_HERE\") { nodes { id name quantity } pageInfo { hasNextPage hasPreviousPage startCursor endCursor } } }"
}
```

**Note:** Replace `YOUR_CURSOR_HERE` with the `endCursor` value from the previous response.

### Get Previous Page (Last 10 Items)

**Request Body:**
```json
{
  "query": "query { items(search: null, last: 10, before: \"YOUR_CURSOR_HERE\") { nodes { id name quantity } pageInfo { hasNextPage hasPreviousPage startCursor } } }"
}
```

**Note:** Replace `YOUR_CURSOR_HERE` with the `startCursor` value from the previous response.

### Combined: Filtering, Sorting, and Pagination

**Request Body:**
```json
{
  "query": "query { items(search: null, first: 10, where: { quantity: { gt: 0 } }, order: [{ quantity: DESC }]) { nodes { id name quantity roomId } pageInfo { hasNextPage endCursor } } }"
}
```

This query:
- Filters items where quantity > 0
- Sorts by quantity (descending)
- Returns first 10 results with pagination info

---

## Mutations

### 1. Create a Location

**Request Body:**
```json
{
  "query": "mutation { addLocation(name: \"My Home\") { id name createdAt } }"
}
```

**Response:**
```json
{
  "data": {
    "addLocation": {
      "id": 1,
      "name": "My Home",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  }
}
```

**Note:** Save the `id` from the response - you'll need it to create rooms.

### 2. Create a Room

**Request Body:**
```json
{
  "query": "mutation { addRoom(name: \"Living Room\", locationId: 1) { id name locationId createdAt } }"
}
```

**Note:** Replace `locationId: 1` with an actual location ID from your database.

**Error Example (Invalid Location ID):**
```json
{
  "errors": [
    {
      "message": "Location with ID 999 not found.",
      "extensions": {
        "code": "LOCATION_NOT_FOUND"
      }
    }
  ]
}
```

### 3. Create an Item

**Request Body:**
```json
{
  "query": "mutation { addItem(name: \"Laptop\", quantity: 2, roomId: 1) { id name quantity roomId createdAt } }"
}
```

**Note:** Replace `roomId: 1` with an actual room ID from your database.

**Error Example (Invalid Room ID):**
```json
{
  "errors": [
    {
      "message": "Room with ID 999 not found."
    }
  ]
}
```

### 4. Move an Item to a Different Room

**Request Body:**
```json
{
  "query": "mutation { moveItem(itemId: 1, newRoomId: 2) { id name quantity roomId createdAt } }"
}
```

**Note:** 
- Replace `itemId: 1` with an actual item ID
- Replace `newRoomId: 2` with the target room ID

**Error Examples:**

Invalid Item ID:
```json
{
  "errors": [
    {
      "message": "Item with ID 999 not found."
    }
  ]
}
```

Invalid New Room ID:
```json
{
  "errors": [
    {
      "message": "Room with ID 999 not found."
    }
  ]
}
```

### 5. Delete an Item

**Request Body:**
```json
{
  "query": "mutation { deleteItem(itemId: 1) }"
}
```

**Success Response:**
```json
{
  "data": {
    "deleteItem": true
  }
}
```

**Error Example:**
```json
{
  "errors": [
    {
      "message": "Item with ID 999 not found."
    }
  ]
}
```

---

## Using Variables in Postman

For more complex queries, you can use GraphQL variables.

### Setup

1. In the **Body** tab, use this format:

```json
{
  "query": "mutation AddItem($name: String!, $quantity: Int!, $roomId: Int!) { addItem(name: $name, quantity: $quantity, roomId: $roomId) { id name quantity roomId createdAt } }",
  "variables": {
    "name": "Gaming Mouse",
    "quantity": 3,
    "roomId": 1
  }
}
```

### Variable Example: Search with Variables

**Request Body:**
```json
{
  "query": "query GetItems($search: String, $first: Int, $where: ItemFilterInput, $order: [ItemSortInput!]) { items(search: $search, first: $first, where: $where, order: $order) { nodes { id name quantity roomId } pageInfo { hasNextPage } } }",
  "variables": {
    "search": null,
    "first": 10,
    "where": {
      "quantity": {
        "gt": 5
      }
    },
    "order": [
      {
        "name": "ASC"
      }
    ]
  }
}
```

---

## Postman Collection Tips

### 1. Create a Collection

Organize your requests in a Postman Collection:

1. Click **New** → **Collection**
2. Name it "StuffTracker GraphQL API"
3. Group requests by type (Queries, Mutations, etc.)

### 2. Use Environment Variables

Create a Postman Environment:

1. Click **Environments** → **+**
2. Add variables:
   - `baseUrl`: `http://localhost:5030`
   - `graphqlEndpoint`: `{{baseUrl}}/graphql`

3. Use in requests:
   - URL: `{{graphqlEndpoint}}`

### 3. Pre-request Scripts

Add scripts to automatically set values:

```javascript
// Set a random room ID (example)
pm.environment.set("roomId", "1");
```

### 4. Test Scripts

Add assertions to validate responses:

```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has data", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.data).to.exist;
});

pm.test("No GraphQL errors", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.errors).to.not.exist;
});
```

---

## Example Workflow

### Complete Workflow: Create Location → Room → Item

#### Step 1: Create Location

**Request:**
```json
{
  "query": "mutation { addLocation(name: \"Office Building\") { id name } }"
}
```

**Response:**
```json
{
  "data": {
    "addLocation": {
      "id": 1,
      "name": "Office Building"
    }
  }
}
```

**Save `id: 1` for next step.**

#### Step 2: Create Room (using location ID from Step 1)

**Request:**
```json
{
  "query": "mutation { addRoom(name: \"Conference Room A\", locationId: 1) { id name locationId } }"
}
```

**Response:**
```json
{
  "data": {
    "addRoom": {
      "id": 1,
      "name": "Conference Room A",
      "locationId": 1
    }
  }
}
```

**Save `id: 1` for next step.**

#### Step 3: Create Item (using room ID from Step 2)

**Request:**
```json
{
  "query": "mutation { addItem(name: \"Projector\", quantity: 1, roomId: 1) { id name quantity roomId } }"
}
```

**Response:**
```json
{
  "data": {
    "addItem": {
      "id": 1,
      "name": "Projector",
      "quantity": 1,
      "roomId": 1
    }
  }
}
```

#### Step 4: Query All Items in the Room

**Request:**
```json
{
  "query": "query { items(search: null, first: 10, where: { roomId: { eq: 1 } }) { nodes { id name quantity roomId } } }"
}
```

---

## Common Issues and Troubleshooting

### Issue: "Could not resolve type..."

**Solution:** Ensure the API is running and the GraphQL endpoint is accessible.

### Issue: "Field 'fieldName' doesn't exist..."

**Solution:** Check the GraphQL schema. Use the GraphQL IDE at `http://localhost:5030/graphql` to explore available fields.

### Issue: "Maximum allowed type cost was exceeded"

**Solution:** Your query is too complex. Reduce the number of requested fields or use pagination (`first: 50` or less).

### Issue: Connection Refused

**Solution:** 
1. Verify the API is running (`dotnet run` in `StuffTracker.Api` directory)
2. Check the port (default: `5030` for HTTP, `7185` for HTTPS)
3. Ensure no firewall is blocking the connection

### Issue: "Room with ID X not found"

**Solution:** Verify the room ID exists. Query rooms first:
```json
{
  "query": "query { locations(first: 10) { nodes { id name } } }"
}
```

---

## GraphQL IDE Alternative

While Postman is excellent for API testing, the built-in GraphQL IDE (Banana Cake Pop) provides a more visual way to explore the API:

1. Start the API
2. Open `http://localhost:5030/graphql` in your browser
3. Use the interactive schema explorer and query builder

---

## Additional Resources

- [GraphQL Documentation](https://graphql.org/learn/)
- [Hot Chocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [Postman GraphQL Guide](https://learning.postman.com/docs/sending-requests/graphql/graphql-overview/)

