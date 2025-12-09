import { useEffect, useRef, useState } from "react";
import type { LiveSummaryMessage, WebSocketState } from "../types/meeting.types";

export const useWebSocket = (sessionId: string | null) => {
	const [state, setState] = useState<WebSocketState>({
		isConnected: false,
		error: null,
		messages: [],
		latestSummary: null,
	});

	const wsRef = useRef<WebSocket | null>(null);

	useEffect(() => {
		if (!sessionId) return;

		try {
			const ws = new WebSocket(`ws://localhost:8280/ws/transcription?sessionId=${sessionId}`);
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

		return () => {
			if (wsRef.current) {
				wsRef.current.close();
			}
		};
	}, [sessionId]);

	return state;
};
