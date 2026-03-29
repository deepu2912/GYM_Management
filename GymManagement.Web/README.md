# GymManagement.Web

React frontend for `GymManagement.Api` using:

- React 18
- Axios
- React Router
- Context API
- Tailwind CSS

## Run

```powershell
cd GymManagement.Web
npm install
npm run dev
```

## Environment

Create `.env` from `.env.example` if you need to override defaults:

- `VITE_API_BASE_URL`: API URL used by Axios (default empty, uses Vite proxy)
- `VITE_PROXY_TARGET`: Vite proxy target for `/api` and `/uploads` (default `http://localhost:5177`)

## Current Routes

- `/login`
- `/register`
- `/` dashboard
- `/members`
- `/plans`
