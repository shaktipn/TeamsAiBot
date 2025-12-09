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
