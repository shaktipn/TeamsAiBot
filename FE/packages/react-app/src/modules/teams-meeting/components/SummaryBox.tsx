import { Card, Typography } from "antd";

const { Paragraph } = Typography;

interface SummaryBoxProps {
	summary: string | null;
}

export const SummaryBox: React.FC<SummaryBoxProps> = ({ summary }) => {
	return (
		<Card title="Latest Summary" style={{ height: "100%" }}>
			{summary ? <Paragraph>{summary}</Paragraph> : <Paragraph type="secondary">Waiting for summary...</Paragraph>}
		</Card>
	);
};
