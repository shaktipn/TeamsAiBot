import { DisconnectOutlined, LinkOutlined } from "@ant-design/icons";
import { PublicLayout } from "@shared/components/layout/PublicLayout";
import { Badge, Button, Col, Row, Space, Typography } from "antd";
import { useSearchParams } from "react-router-dom";
import { MessageList } from "../components/MessageList";
import { SessionError } from "../components/SessionError";
import { SummaryBox } from "../components/SummaryBox";
import { useWebSocket } from "../hooks/useWebSocket";

const { Title, Text } = Typography;

export const MeetingSession: React.FC = () => {
	const [searchParams] = useSearchParams();
	const sessionId = searchParams.get("sessionId");

	const { isConnected, error, messages, latestSummary, connect, disconnect } = useWebSocket(sessionId);

	if (!sessionId) {
		return <SessionError />;
	}

	return (
		<PublicLayout>
			<Space direction="vertical" size="large" style={{ width: "100%" }}>
				<div>
					<Title level={2}>Meeting Session</Title>
					<Space>
						<Text type="secondary">Session ID: {sessionId}</Text>
						<Badge status={isConnected ? "success" : "error"} text={isConnected ? "Connected" : "Disconnected"} />
						{isConnected ? (
							<Button danger icon={<DisconnectOutlined />} onClick={disconnect}>
								Disconnect
							</Button>
						) : (
							<Button type="primary" icon={<LinkOutlined />} onClick={connect}>
								Connect
							</Button>
						)}
					</Space>
					{error && (
						<Text type="danger" style={{ display: "block", marginTop: 8 }}>
							{error}
						</Text>
					)}
				</div>

				<Row gutter={16}>
					<Col xs={24} md={8}>
						<SummaryBox summary={latestSummary} />
					</Col>
					<Col xs={24} md={16}>
						<div style={{ maxHeight: "70vh", overflowY: "auto", paddingRight: 8 }}>
							<MessageList messages={messages} />
						</div>
					</Col>
				</Row>
			</Space>
		</PublicLayout>
	);
};
