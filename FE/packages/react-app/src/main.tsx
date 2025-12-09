import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App.tsx";

const rootElement = document.getElementById("root");

if (rootElement) {
	if (!rootElement.innerHTML) {
		const root = createRoot(rootElement);
		root.render(
			<StrictMode>
				<App />
			</StrictMode>,
		);
	} else {
		console.warn("React app already mounted. Aborting re-render.");
	}
} else {
	console.error('Root element with ID "root" not found in the DOM.');
}
