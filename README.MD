# UserAPI

A microservice for managing FitLife users. The service allows admins, members, and personal trainers to log in, register accounts, retrieve member information, and manage member activation status.

## Endpoints

### Admin

- `POST /api/Admin/login` - Log in as an admin and receive a JWT token
- `POST /api/Admin/register` - Register a new admin

### Member

- `POST /api/Member/login` - Log in as a member and receive a JWT token
- `POST /api/Member/register` - Register a new member
- `GET /api/Member/{memberId}` - Get a specific member by ID
- `GET /api/Member/own` - Get the currently authenticated member using the JWT token
- `PUT /api/Member/deactivate/{memberId}` - Deactivate a member account (Admin, Trainer)
- `PUT /api/Member/activate/{memberId}` - Activate a member account (Admin, Trainer)

### Personal Trainer

- `POST /api/PersonalTrainer/login` - Log in as a personal trainer and receive a JWT token
- `POST /api/PersonalTrainer/register` - Register a new personal trainer (Admin)

## Technologies

- **C#**
- **ASP.NET Core**
- **JWT Authentication**
- **Role-Based Authorization**
- **Microsoft ILogger**
- **Repository Pattern**
