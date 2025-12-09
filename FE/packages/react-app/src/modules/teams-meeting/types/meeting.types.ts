export interface LiveSummaryMessage {
	type: "LIVE_SUMMARY";
	sessionId: string;
	summary: string;
}

export interface WebSocketState {
	isConnected: boolean;
	error: string | null;
	messages: LiveSummaryMessage[];
	latestSummary: string | null;
}
