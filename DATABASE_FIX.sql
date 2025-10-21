-- RASCOR: Fix site assignments for device
-- Run this on your Azure PostgreSQL database

-- 1. First, check what's currently in the assignments table
SELECT * FROM public.assignments;

-- 2. Check what user_id values exist
SELECT DISTINCT user_id FROM public.assignments;

-- 3. Update assignments for your device
-- Replace YOUR_DEVICE_ID with: 3dce6490-28b4-434b-a9dd-9ac8856a277a
UPDATE public.assignments 
SET user_id = '3dce6490-28b4-434b-a9dd-9ac8856a277a'
WHERE site_id IN ('site-001', 'site-002');

-- 4. Verify the update worked
SELECT a.*, s.name as site_name 
FROM public.assignments a
LEFT JOIN public.sites s ON a.site_id = s.id
WHERE a.user_id = '3dce6490-28b4-434b-a9dd-9ac8856a277a';

-- Expected result: 2 rows showing site-001 and site-002 assigned to your device
