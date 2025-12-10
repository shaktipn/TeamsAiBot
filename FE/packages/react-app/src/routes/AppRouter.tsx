import { ProtectedRoute } from "@modules/auth/components/ProtectedRoute";
import { SignIn } from "@modules/auth/pages/SignIn";
import { SignUp } from "@modules/auth/pages/SignUp";
import { MeetingSession } from "@modules/teams-meeting/pages/MeetingSession";
import { Meetings } from "@modules/teams-meeting/pages/Meetings";
import { ROUTES } from "@utils/constants";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { Dashboard } from "../pages/Dashboard";

export const router = createBrowserRouter([
	{
		path: ROUTES.ROOT,
		element: <Navigate to={ROUTES.SIGN_IN} replace />,
	},
	{
		path: ROUTES.SIGN_IN,
		element: <SignIn />,
	},
	{
		path: ROUTES.SIGN_UP,
		element: <SignUp />,
	},
	{
		path: ROUTES.DASHBOARD,
		element: (
			<ProtectedRoute>
				<Dashboard />
			</ProtectedRoute>
		),
	},
	{
		path: ROUTES.MEETINGS,
		element: (
			<ProtectedRoute>
				<Meetings />
			</ProtectedRoute>
		),
	},
	{
		path: ROUTES.MEETING_SESSION,
		element: <MeetingSession />,
	},
]);
