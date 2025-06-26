import { useEffect, useState } from "react";
import "./App.css";
import Layout from "./Layout";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  useLocation,
} from "react-router-dom";
import { ThemeProvider } from "./components/theme-provider";
import Scraper from "./pages/Scraper";
import Home from "./pages/Home";
import Results from "./pages/Results";

function AppRoutes() {
  const location = useLocation();

  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Home key={location.pathname} />} />
        <Route path="/scraper" element={<Scraper key={location.pathname} />} />
        <Route path="/results" element={<Results key={location.pathname} />} />
      </Route>
    </Routes>
  );
}

function App() {
  return (
    <ThemeProvider>
      <Router>
        <AppRoutes />
      </Router>
    </ThemeProvider>
  );
}

export default App;
