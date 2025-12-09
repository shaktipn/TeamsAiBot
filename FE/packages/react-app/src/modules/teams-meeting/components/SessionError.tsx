import { ROUTES } from "@utils/constants";
import { Alert } from "antd";
import { Link } from "react-router-dom";

export const SessionError: React.FC = () => {
	return (
		<Alert
			title="Session ID Required"
			description={
				<div>
					<p>Please provide a valid session ID in the URL query parameter.</p>
					<p>
						Example: <code>/meetings/session?sessionId=550e8400-e29b-41d4-a716-446655440000</code>
					</p>
					<Link to={ROUTES.MEETINGS}>Go back to Meetings</Link>
				</div>
			}
			type="error"
			showIcon
			style={{ margin: "24px" }}
		/>
	);
};
