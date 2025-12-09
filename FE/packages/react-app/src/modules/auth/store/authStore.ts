import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthState, User } from "./types";

export const useAuthStore = create<AuthState>()(
	persist(
		(set) => ({
			user: null,
			isAuthenticated: false,
			isLoading: false,

			signUp: async (email: string, _password: string, name: string) => {
				set({ isLoading: true });

				try {
					await new Promise((resolve) => setTimeout(resolve, 1000));

					const user: User = {
						id: crypto.randomUUID(),
						email,
						name,
						createdAt: new Date(),
					};

					set({
						user,
						isAuthenticated: true,
						isLoading: false,
					});
				} catch (error) {
					set({ isLoading: false });
					throw error;
				}
			},

			signIn: async (email: string, _password: string) => {
				set({ isLoading: true });

				try {
					await new Promise((resolve) => setTimeout(resolve, 1000));

					const user: User = {
						id: crypto.randomUUID(),
						email,
						name: email.split("@")[0],
						createdAt: new Date(),
					};

					set({
						user,
						isAuthenticated: true,
						isLoading: false,
					});
				} catch (error) {
					set({ isLoading: false });
					throw error;
				}
			},

			signOut: () => {
				set({
					user: null,
					isAuthenticated: false,
				});
			},

			initializeAuth: () => {
				// Persist middleware handles rehydration automatically
			},
		}),
		{
			name: "auth-storage",
			partialize: (state) => ({
				user: state.user,
				isAuthenticated: state.isAuthenticated,
			}),
		},
	),
);
