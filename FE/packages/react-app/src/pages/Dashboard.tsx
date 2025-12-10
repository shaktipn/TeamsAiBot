import { FileTextOutlined, SettingOutlined, UserOutlined } from "@ant-design/icons";
import { useAuthStore } from "@modules/auth/store/authStore";
import { DashboardLayout } from "@shared/components/layout/DashboardLayout";
import { Card, Col, Row, Statistic, Typography } from "antd";

const { Title, Paragraph } = Typography;

export const Dashboard: React.FC = () => {
	const user = useAuthStore((state) => state.user);

	return (
		<DashboardLayout>
			<Title level={2}>Welcome back, {user?.email}!</Title>
			<Paragraph type="secondary">Here's what's happening with your account today.</Paragraph>

			<Row gutter={[16, 16]} style={{ marginTop: 24 }}>
				<Col xs={24} sm={12} lg={8}>
					<Card>
						<Statistic title="Profile Completion" value={75} suffix="%" prefix={<UserOutlined />} />
					</Card>
				</Col>

				<Col xs={24} sm={12} lg={8}>
					<Card>
						<Statistic title="Documents" value={12} prefix={<FileTextOutlined />} />
					</Card>
				</Col>

				<Col xs={24} sm={12} lg={8}>
					<Card>
						<Statistic title="Settings Configured" value={8} suffix="/ 10" prefix={<SettingOutlined />} />
					</Card>
				</Col>
			</Row>

			<Card style={{ marginTop: 24 }} title="Recent Activity">
				<Paragraph>No recent activity to display.</Paragraph>
			</Card>
		</DashboardLayout>
	);
};
