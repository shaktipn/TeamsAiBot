import { Layout } from "antd";
import type { ReactNode } from "react";
import { Sidebar } from "./Sidebar";

const { Sider, Content } = Layout;

interface DashboardLayoutProps {
	children: ReactNode;
}

export const DashboardLayout: React.FC<DashboardLayoutProps> = ({ children }) => {
	return (
		<Layout style={{ minHeight: "100vh" }}>
			<Sider
				width={250}
				style={{
					overflow: "auto",
					height: "100vh",
					position: "fixed",
					left: 0,
				}}
			>
				<Sidebar />
			</Sider>
			<Layout style={{ marginLeft: 250 }}>
				<Content style={{ padding: 24, background: "#fff", minHeight: 280 }}>{children}</Content>
			</Layout>
		</Layout>
	);
};
