import { DashboardOutlined, LogoutOutlined, TeamOutlined, UserOutlined, VideoCameraOutlined } from "@ant-design/icons";
import { useAuthStore } from "@modules/auth/store/authStore";
import { Avatar, Menu, Space, Typography } from "antd";
import { useLocation, useNavigate } from "react-router-dom";
import type { NavigationItem } from "../../types/user.types";

const { Text } = Typography;

const navigationItems: NavigationItem[] = [
	{
		key: "dashboard",
		icon: <DashboardOutlined />,
		label: "Dashboard",
		path: "/dashboard",
	},
	{
		key: "meetings",
		icon: <TeamOutlined />,
		label: "Meetings",
		path: "/meetings",
	},
	{
		key: "meeting-session",
		icon: <VideoCameraOutlined />,
		label: "Meeting Session",
		path: "/meetings/session",
	},
];

export const Sidebar: React.FC = () => {
	const navigate = useNavigate();
	const location = useLocation();
	const { user, signOut } = useAuthStore();

	const getSelectedKey = () => {
		const path = location.pathname;
		if (path === "/dashboard") return "dashboard";
		if (path === "/meetings/session" || path.startsWith("/meetings/session")) return "meeting-session";
		if (path.startsWith("/meetings")) return "meetings";
		return "dashboard";
	};

	const handleMenuClick = (path: string) => {
		navigate(path);
	};

	const handleLogout = () => {
		signOut();
		navigate("/signin");
	};

	return (
		<div style={{ height: "100%", display: "flex", flexDirection: "column" }}>
			<div
				style={{
					padding: "16px",
					textAlign: "center",
					borderBottom: "1px solid rgba(255,255,255,0.1)",
				}}
			>
				<Text strong style={{ color: "white", fontSize: 20 }}>
					TeamsAIBot
				</Text>
			</div>

			<Menu theme="dark" mode="inline" selectedKeys={[getSelectedKey()]} style={{ flex: 1, border: "none" }}>
				{navigationItems.map((item) => (
					<Menu.Item key={item.key} icon={item.icon} onClick={() => handleMenuClick(item.path)}>
						{item.label}
					</Menu.Item>
				))}
			</Menu>

			<div
				style={{
					padding: "16px",
					borderTop: "1px solid rgba(255,255,255,0.1)",
					background: "#002140",
				}}
			>
				<Space orientation="vertical" size="small" style={{ width: "100%" }}>
					<Space>
						<Avatar icon={<UserOutlined />} />
						<div>
							<Text style={{ color: "white", display: "block", fontSize: 14 }}>{user?.email}</Text>
						</div>
					</Space>
					<Menu theme="dark" mode="inline" style={{ background: "transparent", border: "none" }}>
						<Menu.Item key="logout" icon={<LogoutOutlined />} onClick={handleLogout}>
							Logout
						</Menu.Item>
					</Menu>
				</Space>
			</div>
		</div>
	);
};
