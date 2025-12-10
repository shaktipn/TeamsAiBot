import { HomeOutlined } from "@ant-design/icons";
import { ROUTES } from "@utils/constants";
import { Button, Layout, Space, Typography } from "antd";
import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";

const { Header, Content } = Layout;
const { Text } = Typography;

interface PublicLayoutProps {
	children: ReactNode;
	showBackButton?: boolean;
}

export const PublicLayout: React.FC<PublicLayoutProps> = ({ children, showBackButton = true }) => {
	const navigate = useNavigate();

	return (
		<Layout style={{ minHeight: "100vh" }}>
			<Header
				style={{
					background: "#001529",
					padding: "0 24px",
					display: "flex",
					alignItems: "center",
					justifyContent: "space-between",
				}}
			>
				<Text strong style={{ color: "white", fontSize: 20 }}>
					TeamsAIBot
				</Text>

				{showBackButton && (
					<Space>
						<Button icon={<HomeOutlined />} onClick={() => navigate(ROUTES.MEETINGS)}>
							Back to Meetings
						</Button>
					</Space>
				)}
			</Header>

			<Content
				style={{
					padding: 24,
					background: "#fff",
					minHeight: "calc(100vh - 64px)",
				}}
			>
				{children}
			</Content>
		</Layout>
	);
};
