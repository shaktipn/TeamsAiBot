import { Card, Space, Typography } from "antd";
import { useEffect, useRef } from "react";
import type { LiveSummaryMessage } from "../types/meeting.types";

const { Text, Paragraph } = Typography;

interface MessageListProps {
	messages: LiveSummaryMessage[];
}

export const MessageList: React.FC<MessageListProps> = ({ messages }) => {
	const bottomRef = useRef<HTMLDivElement>(null);

	// biome-ignore lint/correctness/useExhaustiveDependencies: This is the desired behaviour
	useEffect(() => {
		bottomRef.current?.scrollIntoView({ behavior: "smooth" });
	}, [messages]);

	return (
		<Space orientation="vertical" style={{ width: "100%" }} size="middle">
			{messages.length === 0 ? (
				<Card>
					<Text type="secondary">No messages received yet...</Text>
				</Card>
			) : (
				messages.map((msg, index) => (
					// biome-ignore lint/suspicious/noArrayIndexKey: index is ok here
					<Card key={index} size="small">
						<Space orientation="vertical" size="small" style={{ width: "100%" }}>
							<Text type="secondary" style={{ fontSize: 12 }}>
								Message #{index + 1}
							</Text>
							<Paragraph style={{ marginBottom: 0 }}>{msg.summary}</Paragraph>
						</Space>
					</Card>
				))
			)}
			<div ref={bottomRef} />
		</Space>
	);
};
