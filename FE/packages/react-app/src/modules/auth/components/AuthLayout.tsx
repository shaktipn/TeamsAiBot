import { Card, Layout, Typography } from "antd";
import type { ReactNode } from "react";

const { Content } = Layout;
const { Title } = Typography;

interface AuthLayoutProps {
	children: ReactNode;
	title: string;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children, title }) => {
	return (
		<Layout style={{ minHeight: "100vh", background: "#f0f2f5" }}>
			<Content
				style={{
					display: "flex",
					justifyContent: "center",
					alignItems: "center",
				}}
			>
				<Card style={{ width: 450, boxShadow: "0 4px 12px rgba(0,0,0,0.1)" }}>
					<Title level={2} style={{ textAlign: "center", marginBottom: 32 }}>
						{title}
					</Title>
					{children}
				</Card>
			</Content>
		</Layout>
	);
};
