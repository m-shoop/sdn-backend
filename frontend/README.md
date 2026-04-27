# sdn-frontend

SvelteKit frontend for a nail salon scheduling application. Allows clients to browse and book appointments, backed by the [sdn-backend](https://github.com/m-shoop/sdn-backend) ASP.NET Core API.

## Tech Stack

- **Framework:** SvelteKit
- **Language:** TypeScript / Svelte
- **Styling:** CSS
- **Tooling:** Vite, ESLint, Prettier

## Features

- Appointment booking interface for salon clients
- Communicates with the sdn-backend REST API

## Getting Started

### Prerequisites

- [Node.js](https://nodejs.org/) (v18 or later recommended)
- A running instance of [sdn-backend](https://github.com/m-shoop/sdn-backend)

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/m-shoop/sdn-frontend.git
   cd sdn-frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

The app will be available at `http://localhost:5173` by default.

### Build for Production

```bash
npm run build
npm run preview
```

## Project Structure

```
src/        # Application source code (routes, components, logic)
static/     # Static assets (images, fonts, etc.)
```

## Related

- **Backend:** [sdn-backend](https://github.com/m-shoop/sdn-backend) — ASP.NET Core 8 API
