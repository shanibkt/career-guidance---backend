# Fix All Missing Tables - Quick Guide

Your chatbot (and possibly other features) don't work because database tables are missing.

## Error You're Seeing
```
Table 'freedb_career_guidence.ChatSessions' doesn't exist
```

## Quick Fix

**Run these SQL files in order on your `freedb_career_guidence` database:**

### 1. ✅ ALREADY DONE
- `safe_procs.sql` - Profile procedures

### 2. 🔥 CRITICAL - Do This Now
**File**: `sql/create_chat_tables.sql`

This fixes: ❌ Chatbot not working

### 3. 📚 For Other Features

If other features don't work, run these:

- **Career Suggestions**: `create_career_tables.sql` + `add_comprehensive_it_careers.sql`
- **Learning Path**: `create_learning_videos_table.sql`  
- **Progress Tracking**: `create_progress_tables.sql`
- **Jobs**: `01_job_tables_migration.sql`
- **Quiz**: `update_quiz_system.sql`

## How to Run SQL Files

### Option 1: Web Interface (Easiest)
1. Go to your database provider's phpMyAdmin/web interface
2. Select database: `freedb_career_guidence`
3. Go to SQL tab
4. Open the `.sql` file in a text editor
5. Copy all contents
6. Paste into SQL tab
7. Click Execute/Go

### Option 2: Import
1. phpMyAdmin → Import tab
2. Choose file
3. Click Go

## What Each Table Does

| Table | Feature | Status |
|-------|---------|--------|
| `userprofiles` | Profile | ✅ Fixed |
| `ChatSessions` | Chatbot | ❌ Missing |
| `ChatMessages` | Chatbot | ❌ Missing |
| `careers` | Recommendations | ❓ Unknown |
| `learning_videos` | Learning Path | ❓ Unknown |
| `user_career_progress` | Progress | ❓ Unknown |
| `Jobs` | Job Search | ❓ Unknown |

## After Creating Tables

1. Hot reload Flutter app (press `r`)
2. Try chatbot again
3. If other features still don't work, check Flutter console for new errors
4. Run the corresponding SQL file for that feature

## Still Having Issues?

Share the **exact error message** from Flutter console for each feature that doesn't work.
