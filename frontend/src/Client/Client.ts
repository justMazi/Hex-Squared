import { Client } from './GeneratedClient';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Custom fetch function that includes credentials
const fetchWithCredentials = (url: RequestInfo, init?: RequestInit): Promise<Response> => {
	const initWithCredentials: RequestInit = {
		...init,
		credentials: 'include' as RequestCredentials
	};
	return fetch(url, initWithCredentials);
};

// Create the client with the custom fetch function
const client = new Client(API_BASE_URL, { fetch: fetchWithCredentials });

export default client;
