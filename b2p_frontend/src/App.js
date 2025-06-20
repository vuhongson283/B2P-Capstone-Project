import logo from "./logo.svg";
import "./App.scss";
import { Outlet } from "react-router-dom";
function App() {
  return (
    <div className="app-container">
      <div className="header-container"></div>

      <div className="main-container">
        <div className="sidenav-container"></div>
        <div className="app-content">
                  <img src={logo} className="App-logo" alt="logo" />
                  <p>
                      Edit <code>src/App.js</code> and save to reload.
                  </p>
        </div>
      </div>
      <div></div>
    </div>
  );
}

export default App;
