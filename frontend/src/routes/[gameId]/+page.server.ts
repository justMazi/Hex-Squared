import type { SessionCookieData } from './SessionCookieData';

export async function load({ cookies }) {
	const hexSession = cookies.get('hex_session');
	let sessionData = null;
	console.log('hexSession:', hexSession);
	if (hexSession) {
		try {
			const jsonValue = atob(hexSession);

			sessionData = JSON.parse(jsonValue) as SessionCookieData;
		} catch (error) {
			console.error('Error decoding session cookie:', error);
		}
	}

	return {
		props: { sessionData }
	};
}
