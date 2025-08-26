# Dairy Management System - Deployment Version

This is the deployment-ready version of the Dairy Milk Collection & Sales Management System.

## Features

- **Milk Collection Management**: Track daily milk collections from farmers
- **Sales Management**: Manage milk sales to customers
- **Payment Processing**: Handle farmer and customer payments
- **Reporting**: Generate Excel and PDF reports
- **Multi-language Support**: English, Hindi, and Marathi
- **Authentication**: JWT-based authentication system
- **Database**: PostgreSQL with full schema support

## Deployment Options

### 1. Render.com (Recommended)

1. Push this folder to a GitHub repository
2. Connect your GitHub repo to Render
3. Render will automatically detect the `render.yaml` and deploy both the web service and PostgreSQL database
4. The application will be available at your Render URL

### 2. Railway

1. Push to GitHub repository
2. Connect to Railway
3. Set environment variable `DATABASE_URL` to your PostgreSQL connection string
4. Deploy the web service

### 3. Docker

```bash
# Build the Docker image
docker build -t dairy-management .

# Run with environment variables
docker run -p 5000:5000 \
  -e DATABASE_URL="your-postgres-connection-string" \
  -e JWT_KEY="your-jwt-secret-key" \
  dairy-management
```

## Environment Variables

- `DATABASE_URL`: PostgreSQL connection string
- `JWT_KEY`: Secret key for JWT token generation
- `JWT_ISSUER`: JWT token issuer (default: DairyManagement)
- `JWT_AUDIENCE`: JWT token audience (default: DairyUsers)
- `ASPNETCORE_ENVIRONMENT`: Environment (Production/Development)
- `PORT`: Port number (default: 5000)

## Database Setup

The application will automatically connect to the PostgreSQL database. Make sure your database has the `dairy` schema created.

For Railway PostgreSQL, the connection string format is automatically handled.

## API Endpoints

- `GET /health` - Health check
- `GET /api/test-db` - Database connection test
- `GET /api/milk-collections` - List milk collections
- `POST /api/milk-collections` - Add milk collection
- `GET /api/sales` - List sales
- `POST /api/sales` - Add sale

## Web Interface

- `/` - Redirects to dashboard
- `/dashboard` - Main dashboard
- `/simple-login` - Login page
- `/milk-collections` - Milk collection management
- `/sales` - Sales management
- `/reports` - Reports and analytics

## Technology Stack

- **.NET 8**: Web framework
- **PostgreSQL**: Database
- **Serilog**: Logging
- **QuestPDF**: PDF generation
- **ClosedXML**: Excel generation
- **JWT**: Authentication
- **Swagger**: API documentation

## Support

For issues or questions, refer to the main project documentation or contact the maintainer.