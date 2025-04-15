
import { test, expect } from '@playwright/test';

test('POST BS-Select episode to endpoint', async ({ request }) => {
  // Clear any events from previous test runs
  await request.delete('http://localhost:7010/api/events');

  const triggerResponse = await request.post(`http://localhost:7002/api/bsselect/episodes/ingress`, {
    data: {
      episode_id: "123",
      nhs_number: "9990000000",
      date_of_birth: "1970-01-01",
      first_given_name: "Test",
      family_name: "User"
    },
    headers: {
      'Content-Type': 'application/json',
    }
  });
  expect(triggerResponse.ok()).toBeTruthy();

  // 3000 milliseconds works for now but may need to increase this value in the future
  await new Promise(resolve => setTimeout(resolve, 3000));

  const eventsResponse = await request.get('http://localhost:7010/api/events');
  const events: any[] = await eventsResponse.json();

  expect(events.length).toBe(1);
  expect(events[0].source).toBe('ServiceLayer');
  expect(events[0].type).toBe('EpisodeEvent');
  expect(events[0].data.PathwayTypeId).toBe('11111111-1111-1111-1111-111111111113');
  expect(events[0].data.PathwayTypeName).toBe('Breast Screening Routine');
  expect(events[0].data.ScreeningName).toBe('Breast Screening');
  expect(events[0].data.NhsNumber).toBe('9990000000');
  expect(events[0].data.DOB).toBe('1970-01-01');
  expect(events[0].data.Name).toBe('Test User');
});
