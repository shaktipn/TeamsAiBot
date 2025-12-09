import { LockOutlined, MailOutlined } from "@ant-design/icons";
import { ROUTES } from "@utils/constants";
import { Button, Form, Input, message } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "../components/AuthLayout";
import { useAuthStore } from "../store/authStore";
import type { SignInFormData } from "../types/auth.types";

export const SignIn: React.FC = () => {
	const navigate = useNavigate();
	const { signIn, isLoading } = useAuthStore();
	const [form] = Form.useForm();

	const onFinish = async (values: SignInFormData) => {
		try {
			await signIn(values.email, values.password);
			message.success("Signed in successfully!");
			navigate(ROUTES.DASHBOARD);
		} catch (_error) {
			message.error("Invalid credentials. Please try again.");
		}
	};

	return (
		<AuthLayout title="Sign In">
			<Form form={form} name="signin" onFinish={onFinish} layout="vertical" requiredMark={false}>
				<Form.Item
					name="email"
					label="Email"
					rules={[
						{ required: true, message: "Please enter your email" },
						{ type: "email", message: "Please enter a valid email" },
					]}
				>
					<Input prefix={<MailOutlined />} placeholder="john@example.com" size="large" />
				</Form.Item>

				<Form.Item name="password" label="Password" rules={[{ required: true, message: "Please enter your password" }]}>
					<Input.Password prefix={<LockOutlined />} placeholder="Enter password" size="large" />
				</Form.Item>

				<Form.Item style={{ marginBottom: 8 }}>
					<Button type="primary" htmlType="submit" size="large" block loading={isLoading}>
						Sign In
					</Button>
				</Form.Item>

				<div style={{ textAlign: "center" }}>
					Don't have an account? <Link to={ROUTES.SIGN_UP}>Sign up</Link>
				</div>
			</Form>
		</AuthLayout>
	);
};
