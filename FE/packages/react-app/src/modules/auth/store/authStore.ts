import { LeoEmailId } from "@surya-digital/leo-ts-types";
import { SignInWebRPC } from "@teamsaibot/teamsaibot-rpcs/lib/auth/signInWebRPC";
import { SignInWebRPCClientImpl } from "@teamsaibot/teamsaibot-rpcs/lib/auth/signInWebRPCClientImpl";
import { SignUpWebRPC } from "@teamsaibot/teamsaibot-rpcs/lib/auth/signUpWebRPC";
import { SignUpWebRPCClientImpl } from "@teamsaibot/teamsaibot-rpcs/lib/auth/signUpWebRPCClientImpl";
import { getAPIClient } from "@utils/api";
import { create } from "zustand";
import { persist } from "zustand/middleware";
import { logger } from "@/utils/logger";
import type { AuthState, User } from "./types";

export const useAuthStore = create<AuthState>()(
	persist(
		(set) => ({
			user: null,
			isAuthenticated: false,
			isLoading: false,

			signUp: async (email: string, password: string) => {
				set({ isLoading: true });
				try {
					const apiClient = getAPIClient();
					const request = new SignUpWebRPC.Request(new LeoEmailId(email), password);
					const { response, error }: { response?: SignUpWebRPC.Response; error?: SignUpWebRPC.Errors.Errors } =
						await new SignUpWebRPCClientImpl(apiClient).execute(request);
					if (response) {
						const user: User = {
							id: crypto.randomUUID(),
							email,
							createdAt: new Date(),
						};
						set({
							user,
							isAuthenticated: true,
							isLoading: false,
						});
					} else {
						throw Error(`${error}`);
					}
				} catch (error) {
					logger.error("Error when singing up", error);
					throw error;
				} finally {
					set({ isLoading: false });
				}
			},

			signIn: async (email: string, password: string) => {
				set({ isLoading: true });

				try {
					const apiClient = getAPIClient();
					const request = new SignInWebRPC.Request(new LeoEmailId(email), password);
					const { response, error }: { response?: SignInWebRPC.Response; error?: SignInWebRPC.Errors.Errors } =
						await new SignInWebRPCClientImpl(apiClient).execute(request);
					if (response) {
						const user: User = {
							id: crypto.randomUUID(),
							email,
							createdAt: new Date(),
						};

						set({
							user,
							isAuthenticated: true,
						});
					} else {
						throw Error(`${error}`);
					}
				} catch (error) {
					logger.error("Error when singing up", error);
					throw error;
				} finally {
					set({ isLoading: false });
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
