import { LockOutlined, MailOutlined, UserOutlined } from "@ant-design/icons";
import { ROUTES } from "@utils/constants";
import { Button, Form, Input, message } from "antd";
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AuthLayout } from "../components/AuthLayout";
import { PasswordStrength } from "../components/PasswordStrength";
import { useAuthStore } from "../store/authStore";
import type { SignUpFormData } from "../types/auth.types";
import { validatePassword } from "../utils/validation";

export const SignUp: React.FC = () => {
	const navigate = useNavigate();
	const { signUp, isLoading } = useAuthStore();
	const [form] = Form.useForm();
	const [password, setPassword] = useState("");

	const onFinish = async (values: SignUpFormData) => {
		try {
			await signUp(values.email, values.password, values.name);
			message.success("Account created successfully!");
			navigate(ROUTES.DASHBOARD);
		} catch (_error) {
			message.error("Failed to create account. Please try again.");
		}
	};

	return (
		<AuthLayout title="Create Account">
			<Form form={form} name="signup" onFinish={onFinish} layout="vertical" requiredMark={false}>
				<Form.Item name="name" label="Full Name" rules={[{ required: true, message: "Please enter your name" }]}>
					<Input prefix={<UserOutlined />} placeholder="John Doe" size="large" />
				</Form.Item>

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

				<Form.Item
					name="password"
					label="Password"
					rules={[
						{ required: true, message: "Please enter your password" },
						{
							validator: (_, value) => {
								if (!value) return Promise.resolve();
								const validation = validatePassword(value);
								return validation.isValid
									? Promise.resolve()
									: Promise.reject(new Error("Password does not meet requirements"));
							},
						},
					]}
				>
					<Input.Password
						prefix={<LockOutlined />}
						placeholder="Enter password"
						size="large"
						onChange={(e) => setPassword(e.target.value)}
					/>
				</Form.Item>

				<PasswordStrength password={password} />

				<Form.Item
					name="confirmPassword"
					label="Confirm Password"
					dependencies={["password"]}
					rules={[
						{ required: true, message: "Please confirm your password" },
						({ getFieldValue }) => ({
							validator(_, value) {
								if (!value || getFieldValue("password") === value) {
									return Promise.resolve();
								}
								return Promise.reject(new Error("Passwords do not match"));
							},
						}),
					]}
				>
					<Input.Password prefix={<LockOutlined />} placeholder="Confirm password" size="large" />
				</Form.Item>

				<Form.Item style={{ marginBottom: 8 }}>
					<Button type="primary" htmlType="submit" size="large" block loading={isLoading}>
						Sign Up
					</Button>
				</Form.Item>

				<div style={{ textAlign: "center" }}>
					Already have an account? <Link to={ROUTES.SIGN_IN}>Sign in</Link>
				</div>
			</Form>
		</AuthLayout>
	);
};
