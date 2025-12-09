import { DashboardLayout } from "@shared/components/layout/DashboardLayout";
import { Card, Col, Row, Typography } from "antd";

const { Title, Paragraph } = Typography;

export const Meetings: React.FC = () => {
	return (
		<DashboardLayout>
			<Title level={2}>Meetings</Title>
			<Paragraph type="secondary">Join or manage your meeting sessions here.</Paragraph>

			<Row gutter={[16, 16]} style={{ marginTop: 24 }}>
				<Col xs={24} sm={12} lg={8}>
					<Card title="Active Sessions" hoverable>
						<Paragraph>0 active sessions</Paragraph>
					</Card>
				</Col>

				<Col xs={24} sm={12} lg={8}>
					<Card title="Recent Meetings" hoverable>
						<Paragraph>No recent meetings</Paragraph>
					</Card>
				</Col>

				<Col xs={24} sm={12} lg={8}>
					<Card title="Scheduled" hoverable>
						<Paragraph>No scheduled meetings</Paragraph>
					</Card>
				</Col>
			</Row>

			<Card style={{ marginTop: 24 }} title="How to Join a Session">
				<Paragraph>To join a meeting session, use the following URL format:</Paragraph>
				<Paragraph>
					<code>/meetings/session?sessionId=YOUR_SESSION_ID</code>
				</Paragraph>
			</Card>
		</DashboardLayout>
	);
};
