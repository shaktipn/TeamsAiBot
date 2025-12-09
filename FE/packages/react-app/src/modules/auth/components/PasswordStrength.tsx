import { CheckCircleOutlined, CloseCircleOutlined } from "@ant-design/icons";
import { Space, Typography } from "antd";
import type { PasswordValidation } from "../types/auth.types";
import { validatePassword } from "../utils/validation";

const { Text } = Typography;

interface PasswordStrengthProps {
	password: string;
}

export const PasswordStrength: React.FC<PasswordStrengthProps> = ({ password }) => {
	const validation: PasswordValidation = validatePassword(password);

	if (!password) return null;

	const requirements = [
		{
			key: "minLength",
			label: "At least 8 characters",
			met: validation.minLength,
		},
		{
			key: "hasUppercase",
			label: "One uppercase letter",
			met: validation.hasUppercase,
		},
		{
			key: "hasLowercase",
			label: "One lowercase letter",
			met: validation.hasLowercase,
		},
		{ key: "hasDigit", label: "One digit", met: validation.hasDigit },
		{
			key: "hasSymbol",
			label: "One special character",
			met: validation.hasSymbol,
		},
	];

	return (
		<Space direction="vertical" size="small" style={{ width: "100%", marginTop: 8 }}>
			{requirements.map((req) => (
				<Text key={req.key} type={req.met ? "success" : "secondary"} style={{ fontSize: 12 }}>
					{req.met ? <CheckCircleOutlined /> : <CloseCircleOutlined />} {req.label}
				</Text>
			))}
		</Space>
	);
};
