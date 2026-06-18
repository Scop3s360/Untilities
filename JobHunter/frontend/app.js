const API_URL = 'http://127.0.0.1:8000';

// Navigation
document.querySelectorAll('.nav-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
        // Update buttons
        document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active');

        // Update views
        const targetId = e.target.getAttribute('data-target');
        document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
        document.getElementById(targetId).classList.add('active');

        // Fetch data if needed
        if (targetId === 'dashboard') fetchJobs();
        if (targetId === 'profile') fetchProfile();
    });
});

// Fetch Jobs
async function fetchJobs() {
    const grid = document.getElementById('job-grid');
    grid.innerHTML = '<div class="loader">Loading jobs...</div>';
    
    try {
        const response = await fetch(`${API_URL}/jobs`);
        const jobs = await response.json();
        
        if (jobs.length === 0) {
            grid.innerHTML = '<p style="color:var(--text-muted)">No jobs found. Try triggering a scrape!</p>';
            return;
        }

        grid.innerHTML = jobs.map(job => `
            <div class="job-card">
                <h3>${job.title}</h3>
                <div class="company">${job.company}</div>
                <div class="details">
                    <span>📍 ${job.location || 'Remote'}</span>
                    <span>💰 ${job.salary_max ? '£' + job.salary_max : 'Unspecified'}</span>
                </div>
                <div class="description">${job.description || 'No description provided.'}</div>
                <div class="actions">
                    <span style="font-size:12px;color:var(--text-muted)">${job.source}</span>
                    <a href="${job.url}" target="_blank" class="job-link">View Job</a>
                </div>
            </div>
        `).join('');
    } catch (err) {
        grid.innerHTML = `<p style="color:red">Failed to load jobs: ${err.message}</p>`;
    }
}

// Fetch Profile
async function fetchProfile() {
    try {
        const response = await fetch(`${API_URL}/profile`);
        if (response.ok) {
            const profile = await response.json();
            document.getElementById('profile-details').classList.remove('hidden');
            document.getElementById('p-name').innerText = profile.name || 'N/A';
            document.getElementById('p-skills').innerText = profile.skills ? profile.skills.join(', ') : 'N/A';
            document.getElementById('p-clearance').innerText = profile.clearance_status || 'N/A';
        }
    } catch (err) {
        console.log("No profile found or error fetching profile");
    }
}

// Trigger Scrape
document.getElementById('trigger-scrape-btn').addEventListener('click', async (e) => {
    const btn = e.target;
    const originalText = btn.innerText;
    btn.innerText = 'Scraping...';
    btn.disabled = true;

    try {
        await fetch(`${API_URL}/trigger-scrape`, { method: 'POST' });
        // Since it's async background, we just notify
        alert("Scrape triggered in the background! Jobs will appear shortly.");
    } catch (err) {
        alert("Failed to trigger scrape.");
    } finally {
        btn.innerText = originalText;
        btn.disabled = false;
    }
});

// Parse CV
document.getElementById('profile-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('parse-cv-btn');
    const fileInput = document.getElementById('cv-file');
    const file = fileInput.files[0];
    
    if (!file) return alert("Please select a CV file first.");

    btn.innerText = 'Parsing & Updating...';
    btn.disabled = true;

    try {
        const formData = new FormData();
        formData.append("file", file);

        const response = await fetch(`${API_URL}/upload-cv`, {
            method: 'POST',
            body: formData
        });
        
        if (response.ok) {
            alert("Profile updated successfully!");
            fetchProfile();
        } else {
            const err = await response.json();
            alert("Error: " + JSON.stringify(err));
        }
    } catch (err) {
        alert("Failed to parse CV.");
    } finally {
        btn.innerText = 'Parse CV & Update Profile';
        btn.disabled = false;
    }
});

// Initial load
fetchJobs();
