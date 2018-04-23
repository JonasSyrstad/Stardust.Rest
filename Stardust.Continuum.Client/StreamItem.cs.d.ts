declare module server {
	interface streamItem {
		/** This is set by the service if not provided */
		timestamp?: Date;
		userName: string;
		correlationToken: string;
		message: string;
		stackTrace: string;
		logLevel: any;
		serviceName: string;
		environment: string;
		properties: { [index: string]: any };
	}
}
