export * from './chatter.service';
import { ChatterService } from './chatter.service';
export * from './values.service';
import { ValuesService } from './values.service';
export const APIS = [ChatterService, ValuesService];
