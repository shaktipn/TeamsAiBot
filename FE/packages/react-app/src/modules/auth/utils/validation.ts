import type { PasswordValidation } from "../types/auth.types";

export const validatePassword = (password: string): PasswordValidation => {
	return {
		minLength: password.length >= 8,
		hasUppercase: /[A-Z]/.test(password),
		hasLowercase: /[a-z]/.test(password),
		hasDigit: /\d/.test(password),
		hasSymbol: /[!@#$%^&*(),.?":{}|<>]/.test(password),
		isValid:
			password.length >= 8 &&
			/[A-Z]/.test(password) &&
			/[a-z]/.test(password) &&
			/\d/.test(password) &&
			/[!@#$%^&*(),.?":{}|<>]/.test(password),
	};
};

export const validateEmail = (email: string): boolean => {
	const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
	return emailRegex.test(email);
};
