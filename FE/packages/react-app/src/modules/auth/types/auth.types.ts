export interface SignUpFormData {
	email: string;
	password: string;
	confirmPassword: string;
}

export interface SignInFormData {
	email: string;
	password: string;
}

export interface PasswordValidation {
	minLength: boolean;
	hasUppercase: boolean;
	hasLowercase: boolean;
	hasDigit: boolean;
	hasSymbol: boolean;
	isValid: boolean;
}
