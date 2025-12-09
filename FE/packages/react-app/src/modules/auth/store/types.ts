export interface User {
	id: string;
	email: string;
	name: string;
	createdAt: Date;
}

export interface AuthState {
	user: User | null;
	isAuthenticated: boolean;
	isLoading: boolean;

	signUp: (email: string, password: string, name: string) => Promise<void>;
	signIn: (email: string, password: string) => Promise<void>;
	signOut: () => void;
	initializeAuth: () => void;
}
