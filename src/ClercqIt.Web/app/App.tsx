import HomePage from "./home/page";
import PortfolioPage from "./portfolio/page";
import { Routes, Route } from "react-router-dom";

const App = () => (
  <div>
    <Routes>
      <Route path="/" element={<HomePage />}></Route>
      <Route path="/portfolio" element={<PortfolioPage />}></Route>
    </Routes>
  </div>
);