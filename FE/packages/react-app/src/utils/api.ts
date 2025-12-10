import { type APIClient, AxiosAPIClient, Headers } from "@surya-digital/tedwig";
import { API_BASE_URL } from "./constants";
import { logger } from "./logger";

let apiClientInstance: APIClient | null = null;

/**
 * Get or create the singleton API client instance
 * @returns The API client instance
 */
export const getAPIClient = (): APIClient => {
	if (!apiClientInstance) {
		const baseUrl = API_BASE_URL;

		if (!baseUrl) {
			logger.error("API client base URL not found");
			throw new Error("API client base URL not found");
		}

		logger.info("Creating API Client");

		apiClientInstance = new AxiosAPIClient({
			baseURL: new URL(baseUrl),
			timeoutMS: 30000,
			defaultHeaders: new Headers([
				{
					name: "content-type",
					value: "application/json;charset=UTF-8",
				},
			]),
			defaultErrorInterceptor: (error: Error): void => {
				logger.error(`API Error: ${error.message}`);
				// Global error handling can be added here
			},
		});
	}

	return apiClientInstance;
};
