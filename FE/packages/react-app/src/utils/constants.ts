export const ROUTES = {
	ROOT: "/",
	SIGN_IN: "/signin",
	SIGN_UP: "/signup",
	DASHBOARD: "/dashboard",
	MEETINGS: "/meetings",
	MEETING_SESSION: "/meetings/session",
} as const;

export const PASSWORD_REQUIREMENTS = {
	MIN_LENGTH: 8,
	RULES: [
		"At least 8 characters",
		"One uppercase letter",
		"One lowercase letter",
		"One digit",
		"One special character",
	],
} as const;

export const LIVE_SUMMARY_ENDPOINT = "ws://localhost:8280/ws/transcription";
export const API_BASE_URL = "http://localhost:8280";
