import { useCallback, useEffect, useRef, useState } from "react";
import { LIVE_SUMMARY_ENDPOINT } from "@/utils/constants";
import type { LiveSummaryMessage, WebSocketState } from "../types/meeting.types";

interface UseWebSocketReturn extends WebSocketState {
	connect: () => void;
	disconnect: () => void;
}

export const useWebSocket = (sessionId: string | null): UseWebSocketReturn => {
	const [state, setState] = useState<WebSocketState>({
		isConnected: false,
		error: null,
		messages: [],
		latestSummary: null,
	});

	const wsRef = useRef<WebSocket | null>(null);

	const disconnect = useCallback(() => {
		if (wsRef.current) {
			wsRef.current.close();
			wsRef.current = null;
		}
	}, []);

	const connect = useCallback(() => {
		if (!sessionId) {
			setState((prev) => ({ ...prev, error: "No session ID provided" }));
			return;
		}

		// Close existing connection if any
		disconnect();

		try {
			const ws = new WebSocket(`${LIVE_SUMMARY_ENDPOINT}?sessionId=${sessionId}`);
			wsRef.current = ws;

			ws.onopen = () => {
				setState((prev) => ({ ...prev, isConnected: true, error: null }));
			};

			ws.onmessage = (event) => {
				try {
					const data: LiveSummaryMessage = JSON.parse(event.data);

					if (data.type === "LIVE_SUMMARY" && data.sessionId === sessionId) {
						setState((prev) => ({
							...prev,
							messages: [...prev.messages, data],
							latestSummary: data.summary,
						}));
					}
				} catch (err) {
					console.error("Failed to parse message:", err);
				}
			};

			ws.onerror = () => {
				setState((prev) => ({ ...prev, error: "WebSocket connection error" }));
			};

			ws.onclose = () => {
				setState((prev) => ({ ...prev, isConnected: false }));
			};
		} catch (_err) {
			setState((prev) => ({
				...prev,
				error: "Failed to create WebSocket connection",
			}));
		}
	}, [sessionId, disconnect]);

	// Auto-connect on mount if sessionId is available
	useEffect(() => {
		if (sessionId) {
			connect();
		}

		return () => {
			disconnect();
		};
	}, [sessionId, connect, disconnect]);

	return {
		...state,
		connect,
		disconnect,
	};
};
