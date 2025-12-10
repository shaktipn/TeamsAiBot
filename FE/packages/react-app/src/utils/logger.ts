interface Logger {
	info: (msg: string, ...args: unknown[]) => void;
	debug: (msg: string, ...args: unknown[]) => void;
	warn: (msg: string, ...args: unknown[]) => void;
	error: (msg: string, ...args: unknown[]) => void;
}

export const logger: Logger = {
	info: (msg: string, ...args: unknown[]): void => {
		console.info(msg, ...args);
	},

	debug: (msg: string, ...args: unknown[]): void => {
		console.debug(msg, ...args);
	},

	warn: (msg: string, ...args: unknown[]): void => {
		console.warn(msg, ...args);
	},

	error: (msg: string, ...args: unknown[]): void => {
		console.error(msg, ...args);
	},
};
