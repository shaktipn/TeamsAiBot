export interface User {
	id: string;
	email: string;
	name: string;
	createdAt: Date;
}

export interface NavigationItem {
	key: string;
	icon: React.ReactNode;
	label: string;
	path: string;
}
